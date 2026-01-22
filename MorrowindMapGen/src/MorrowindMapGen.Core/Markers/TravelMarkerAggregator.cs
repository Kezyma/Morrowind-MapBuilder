using System.Text.Json;
using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.Markers.Models;
using MorrowindMapGen.Core.Models.TES3;

namespace MorrowindMapGen.Core.Markers;

/// <summary>
/// Aggregates fast travel data from multiple plugins processed in load order.
/// Extracts NPCs with travel destinations and resolves their positions.
/// </summary>
public class TravelMarkerAggregator
{
    private readonly ILogger<TravelMarkerAggregator> _logger;

    /// <summary>
    /// World units per cell (8192 units = 1 cell).
    /// </summary>
    private const double UnitsPerCell = 8192.0;

    /// <summary>
    /// NPCs with travel destinations keyed by ID. Later plugins overwrite earlier ones.
    /// </summary>
    private readonly Dictionary<string, TES3Npc> _npcsWithTravel = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// NPC placements: maps NPC ID to (cellName, translationX, translationY).
    /// </summary>
    private readonly Dictionary<string, (string cellName, double x, double y)> _npcPlacements = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Cell lookup: maps cell name to cell data (for interior/exterior checks).
    /// </summary>
    private readonly Dictionary<string, TES3Cell> _cells = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Door connections from interior cells to their destinations.
    /// Maps (cellName) to list of (destinationCellName, destX, destY).
    /// </summary>
    private readonly Dictionary<string, List<(string destCell, double destX, double destY)>> _doorConnections = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Count of plugins processed.
    /// </summary>
    public int PluginsProcessed { get; private set; }

    public TravelMarkerAggregator(ILogger<TravelMarkerAggregator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Process a single plugin's JSON file, updating accumulated travel data.
    /// Call this for each plugin in load order.
    /// </summary>
    /// <param name="jsonPath">Path to the tes3conv JSON file.</param>
    public void ProcessPlugin(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("Plugin JSON file not found: {Path}", jsonPath);
            return;
        }

        var pluginName = Path.GetFileNameWithoutExtension(jsonPath);
        _logger.LogDebug("Processing plugin for travel data: {Plugin}", pluginName);

        var jsonContent = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        int npcsAdded = 0;
        int placementsAdded = 0;
        int doorsAdded = 0;

        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Expected JSON array at root in {Path}", jsonPath);
                return;
            }

            // First pass: collect all NPCs and Cells
            foreach (var element in root.EnumerateArray())
            {
                if (!element.TryGetProperty("type", out var typeProp))
                    continue;

                var recordType = typeProp.GetString();

                if (string.Equals(recordType, "Npc", StringComparison.OrdinalIgnoreCase))
                {
                    var npc = JsonSerializer.Deserialize<TES3Npc>(element.GetRawText(), options);
                    if (npc?.HasTravelDestinations == true && !string.IsNullOrEmpty(npc.Id))
                    {
                        _npcsWithTravel[npc.Id] = npc;
                        npcsAdded++;
                    }
                }
                else if (string.Equals(recordType, "Cell", StringComparison.OrdinalIgnoreCase))
                {
                    var cell = JsonSerializer.Deserialize<TES3Cell>(element.GetRawText(), options);
                    if (cell != null)
                    {
                        // Store cell by name for interior cells, by grid coords for exterior
                        var cellKey = cell.IsInterior
                            ? cell.Name ?? cell.Id ?? ""
                            : $"({cell.Data?.GridX ?? 0}, {cell.Data?.GridY ?? 0})";

                        if (!string.IsNullOrEmpty(cellKey))
                        {
                            _cells[cellKey] = cell;
                        }

                        // Detect Propylon Chamber cells
                        if (cell.IsInterior && !string.IsNullOrEmpty(cell.Name) &&
                            cell.Name.Contains("Propylon Chamber", StringComparison.OrdinalIgnoreCase))
                        {
                            _propylonChambers[cell.Name] = cell;
                        }

                        // Process references for NPC placements and door connections
                        if (cell.References != null)
                        {
                            foreach (var reference in cell.References)
                            {
                                if (reference.IsDeleted || string.IsNullOrEmpty(reference.Id))
                                    continue;

                                // Track NPC placements (we'll check if they're travel NPCs later)
                                _npcPlacements[reference.Id] = (
                                    cell.IsInterior ? (cell.Name ?? cell.Id ?? "") : $"({cell.Data?.GridX ?? 0}, {cell.Data?.GridY ?? 0})",
                                    reference.TranslationX,
                                    reference.TranslationY
                                );
                                placementsAdded++;

                                // Track door connections for interior->exterior tracing
                                if (reference.HasDestination && reference.Destination?.Cell != null)
                                {
                                    var sourceCellKey = cell.IsInterior
                                        ? cell.Name ?? cell.Id ?? ""
                                        : $"({cell.Data?.GridX ?? 0}, {cell.Data?.GridY ?? 0})";

                                    if (!_doorConnections.TryGetValue(sourceCellKey, out var connections))
                                    {
                                        connections = [];
                                        _doorConnections[sourceCellKey] = connections;
                                    }

                                    var destCell = reference.Destination.Cell;
                                    var destX = reference.Destination.TranslationX;
                                    var destY = reference.Destination.TranslationY;

                                    // Check if destination cell is interior or exterior
                                    // Empty cell name means exterior (position determines cell)
                                    if (string.IsNullOrEmpty(destCell))
                                    {
                                        destCell = $"EXTERIOR:{destX / UnitsPerCell:F0},{destY / UnitsPerCell:F0}";
                                    }

                                    connections.Add((destCell, destX, destY));
                                    doorsAdded++;
                                }
                            }
                        }
                    }
                }
            }

            PluginsProcessed++;
            _logger.LogDebug("Plugin {Plugin}: {Npcs} travel NPCs, {Placements} placements, {Doors} door connections",
                pluginName, npcsAdded, placementsAdded, doorsAdded);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Failed to parse JSON from {Path}: {Message}", jsonPath, ex.Message);
        }
    }

    /// <summary>
    /// Get the aggregated travel data after all plugins have been processed.
    /// </summary>
    /// <returns>Collection of travel nodes and routes.</returns>
    public TravelData GetTravelData()
    {
        _logger.LogInformation("Building travel data from {PluginCount} plugins: {NpcCount} travel NPCs",
            PluginsProcessed, _npcsWithTravel.Count);

        var result = new TravelData();
        var rawRoutes = new List<TravelRoute>();

        foreach (var npc in _npcsWithTravel.Values)
        {
            if (string.IsNullOrEmpty(npc.Id))
                continue;

            // Find NPC placement
            if (!_npcPlacements.TryGetValue(npc.Id, out var placement))
            {
                _logger.LogDebug("No placement found for travel NPC: {Id}", npc.Id);
                continue;
            }

            // Determine travel type
            var travelType = DetermineTravelType(npc);

            // Resolve position (trace to exterior if in interior)
            var (gridX, gridY, isInterior) = ResolvePosition(placement.cellName, placement.x, placement.y);

            if (double.IsNaN(gridX) || double.IsNaN(gridY))
            {
                _logger.LogDebug("Could not resolve position for travel NPC: {Id} in {Cell}", npc.Id, placement.cellName);
                continue;
            }

            var node = new TravelNode
            {
                NpcId = npc.Id,
                NpcName = npc.Name ?? npc.Id,
                CellName = placement.cellName,
                Type = travelType,
                GridX = gridX,
                GridY = gridY,
                IsInInterior = isInterior
            };
            result.Nodes.Add(node);

            // Add routes for each destination
            if (npc.TravelDestinations != null)
            {
                foreach (var dest in npc.TravelDestinations)
                {
                    // Calculate destination grid position
                    double destGridX, destGridY;

                    if (dest.IsExterior)
                    {
                        // Direct exterior coordinates
                        destGridX = dest.TranslationX / UnitsPerCell;
                        destGridY = dest.TranslationY / UnitsPerCell;
                    }
                    else
                    {
                        // Interior destination - trace to exterior
                        var (dx, dy, _) = ResolvePosition(dest.Cell!, dest.TranslationX, dest.TranslationY);
                        destGridX = dx;
                        destGridY = dy;
                    }

                    if (double.IsNaN(destGridX) || double.IsNaN(destGridY))
                    {
                        _logger.LogDebug("Could not resolve destination position for route from {Id} to {Cell}",
                            npc.Id, dest.Cell);
                        continue;
                    }

                    var route = new TravelRoute
                    {
                        FromNpcId = npc.Id,
                        ToCell = dest.Cell ?? $"({destGridX:F0}, {destGridY:F0})",
                        Type = travelType,
                        FromGridX = gridX,
                        FromGridY = gridY,
                        ToGridX = destGridX,
                        ToGridY = destGridY
                    };
                    rawRoutes.Add(route);
                }
            }
        }

        // Consolidate reciprocal routes to connect NPCs directly
        result.Routes = ConsolidateRoutes(rawRoutes, result.Nodes);

        // Add Propylon chambers detected from cell data
        AddPropylonChambers(result);

        _logger.LogInformation("Built travel data: {NodeCount} nodes, {RouteCount} routes (consolidated from {RawCount})",
            result.Nodes.Count, result.Routes.Count, rawRoutes.Count);

        return result;
    }

    /// <summary>
    /// Propylon chamber cells detected during plugin processing.
    /// Maps cell name to the cell data.
    /// </summary>
    private readonly Dictionary<string, TES3Cell> _propylonChambers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds Propylon chambers detected from cell data.
    /// Propylon travel uses chambers in ancient Dunmer strongholds.
    /// </summary>
    private void AddPropylonChambers(TravelData data)
    {
        if (_propylonChambers.Count == 0)
            return;

        var propylonNodes = new List<TravelNode>();

        foreach (var (cellName, cell) in _propylonChambers)
        {
            // Extract stronghold name from cell name (e.g., "Hlormaren, Propylon Chamber" -> "Hlormaren")
            var strongholdName = cellName.Split(',')[0].Trim();

            // Resolve exterior position by tracing through doors
            var exteriorPos = TraceToExterior(cellName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            if (exteriorPos == null)
            {
                _logger.LogDebug("Could not resolve exterior position for Propylon chamber: {Cell}", cellName);
                continue;
            }

            var node = new TravelNode
            {
                NpcId = $"propylon_{strongholdName.ToLowerInvariant().Replace(" ", "_")}",
                NpcName = strongholdName,
                CellName = cellName,
                Type = TravelType.Propylon,
                GridX = exteriorPos.Value.gridX,
                GridY = exteriorPos.Value.gridY,
                IsInInterior = true
            };
            propylonNodes.Add(node);
            data.Nodes.Add(node);
        }

        // Try to find connections from activator destinations within each chamber
        foreach (var (cellName, cell) in _propylonChambers)
        {
            if (cell.References == null)
                continue;

            var sourceNode = propylonNodes.FirstOrDefault(n => n.CellName == cellName);
            if (sourceNode == null)
                continue;

            foreach (var reference in cell.References)
            {
                if (reference.IsDeleted || !reference.HasDestination)
                    continue;

                var destCellName = reference.Destination?.Cell;
                if (string.IsNullOrEmpty(destCellName))
                    continue;

                // Check if destination is another Propylon chamber
                var destNode = propylonNodes.FirstOrDefault(n =>
                    n.CellName != null && n.CellName.Equals(destCellName, StringComparison.OrdinalIgnoreCase));

                if (destNode != null && destNode.NpcId != sourceNode.NpcId)
                {
                    // Found a connection between two Propylon chambers
                    data.Routes.Add(new TravelRoute
                    {
                        FromNpcId = sourceNode.NpcId,
                        ToCell = destNode.CellName ?? destNode.NpcName,
                        Type = TravelType.Propylon,
                        FromGridX = sourceNode.GridX,
                        FromGridY = sourceNode.GridY,
                        ToGridX = destNode.GridX,
                        ToGridY = destNode.GridY
                    });
                }
            }
        }

        if (propylonNodes.Count > 0)
        {
            _logger.LogDebug("Added {Count} Propylon chambers from game data", propylonNodes.Count);
        }
    }

    /// <summary>
    /// Distance threshold (in grid cells) to consider two positions as "nearby".
    /// </summary>
    private const double NearbyThreshold = 1.5;

    /// <summary>
    /// Consolidates reciprocal routes (A->near B and B->near A) into single direct routes (A<->B).
    /// </summary>
    private List<TravelRoute> ConsolidateRoutes(List<TravelRoute> rawRoutes, List<TravelNode> nodes)
    {
        // Build lookup of nodes by NPC ID
        var nodesByNpcId = nodes.ToDictionary(n => n.NpcId, StringComparer.OrdinalIgnoreCase);

        // Track which routes have been consolidated
        var consolidatedRoutes = new List<TravelRoute>();
        var processedPairs = new HashSet<string>();

        foreach (var route in rawRoutes)
        {
            // Find ALL nodes of the same type near this route's destination
            var nearbyDestNodes = FindAllNearbyNodes(nodes, route.ToGridX, route.ToGridY, route.Type, route.FromNpcId);

            // Try to find a nearby node that has a return route (preferred)
            TravelNode? matchedNode = null;
            foreach (var candidateNode in nearbyDestNodes)
            {
                var returnRoute = rawRoutes.FirstOrDefault(r =>
                    r.FromNpcId.Equals(candidateNode.NpcId, StringComparison.OrdinalIgnoreCase) &&
                    r.Type == route.Type &&
                    IsNearby(r.ToGridX, r.ToGridY, route.FromGridX, route.FromGridY));

                if (returnRoute != null)
                {
                    matchedNode = candidateNode;
                    break;
                }
            }

            if (matchedNode != null)
            {
                // Found a node with a return route - consolidate
                var pairKey = string.Compare(route.FromNpcId, matchedNode.NpcId, StringComparison.OrdinalIgnoreCase) < 0
                    ? $"{route.FromNpcId}|{matchedNode.NpcId}|{route.Type}"
                    : $"{matchedNode.NpcId}|{route.FromNpcId}|{route.Type}";

                if (!processedPairs.Contains(pairKey))
                {
                    processedPairs.Add(pairKey);

                    // Create consolidated route connecting the two NPCs directly
                    var consolidatedRoute = new TravelRoute
                    {
                        FromNpcId = route.FromNpcId,
                        ToCell = matchedNode.CellName ?? matchedNode.NpcId,
                        Type = route.Type,
                        FromGridX = route.FromGridX,
                        FromGridY = route.FromGridY,
                        ToGridX = matchedNode.GridX,
                        ToGridY = matchedNode.GridY
                    };
                    consolidatedRoutes.Add(consolidatedRoute);
                }
                continue;
            }

            // No reciprocal route found - only snap if there's exactly ONE nearby node
            // (to avoid false connections when multiple NPCs are clustered)
            if (nearbyDestNodes.Count == 1)
            {
                var singleNode = nearbyDestNodes[0];
                var snappedRoute = new TravelRoute
                {
                    FromNpcId = route.FromNpcId,
                    ToCell = singleNode.CellName ?? singleNode.NpcId,
                    Type = route.Type,
                    FromGridX = route.FromGridX,
                    FromGridY = route.FromGridY,
                    ToGridX = singleNode.GridX,
                    ToGridY = singleNode.GridY
                };
                consolidatedRoutes.Add(snappedRoute);
            }
            else
            {
                // Keep the original route (no nearby nodes, or multiple without return routes)
                consolidatedRoutes.Add(route);
            }
        }

        // Remove duplicate routes (same endpoints regardless of direction)
        var uniqueRoutes = new List<TravelRoute>();
        var seenRoutes = new HashSet<string>();

        foreach (var route in consolidatedRoutes)
        {
            // Create a key that's the same regardless of direction
            var key = CreateRouteKey(route);
            if (!seenRoutes.Contains(key))
            {
                seenRoutes.Add(key);
                uniqueRoutes.Add(route);
            }
        }

        return uniqueRoutes;
    }

    /// <summary>
    /// Creates a unique key for a route that's the same regardless of direction.
    /// </summary>
    private static string CreateRouteKey(TravelRoute route)
    {
        // Round to 1 decimal place to handle minor coordinate differences
        var x1 = Math.Round(route.FromGridX, 1);
        var y1 = Math.Round(route.FromGridY, 1);
        var x2 = Math.Round(route.ToGridX, 1);
        var y2 = Math.Round(route.ToGridY, 1);

        // Order endpoints consistently
        if (x1 < x2 || (x1 == x2 && y1 < y2))
        {
            return $"{route.Type}|{x1},{y1}|{x2},{y2}";
        }
        return $"{route.Type}|{x2},{y2}|{x1},{y1}";
    }

    /// <summary>
    /// Finds all nodes of the specified type near the given coordinates.
    /// </summary>
    private static List<TravelNode> FindAllNearbyNodes(List<TravelNode> nodes, double gridX, double gridY, TravelType type, string excludeNpcId)
    {
        return nodes.Where(n =>
            n.Type == type &&
            !n.NpcId.Equals(excludeNpcId, StringComparison.OrdinalIgnoreCase) &&
            IsNearby(n.GridX, n.GridY, gridX, gridY)).ToList();
    }

    /// <summary>
    /// Checks if two positions are nearby (within threshold).
    /// </summary>
    private static bool IsNearby(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        return distance <= NearbyThreshold;
    }

    /// <summary>
    /// Determines the travel type based on NPC class, faction, ID, and placement context.
    /// </summary>
    private TravelType DetermineTravelType(TES3Npc npc)
    {
        var npcClass = npc.Class ?? "";
        var faction = npc.Faction ?? "";
        var id = npc.Id ?? "";
        var name = npc.Name ?? "";

        // Check class first - this is the most reliable indicator
        if (npcClass.Equals("Caravaner", StringComparison.OrdinalIgnoreCase))
            return TravelType.SiltStrider;

        if (npcClass.Equals("Shipmaster", StringComparison.OrdinalIgnoreCase))
            return TravelType.Boat;

        if (npcClass.Equals("Gondolier", StringComparison.OrdinalIgnoreCase))
            return TravelType.Gondola;

        if (npcClass.Equals("Guild Guide", StringComparison.OrdinalIgnoreCase))
            return TravelType.MagesGuild;

        // Fallback: check faction
        if (faction.Contains("Mages Guild", StringComparison.OrdinalIgnoreCase))
            return TravelType.MagesGuild;

        // Fallback: check id/name for keywords
        var combined = $"{id} {name} {npcClass}".ToLowerInvariant();

        if (combined.Contains("strider") || combined.Contains("caravan"))
            return TravelType.SiltStrider;

        if (combined.Contains("ship") || combined.Contains("boat") || combined.Contains("sailor"))
            return TravelType.Boat;

        if (combined.Contains("gondol"))
            return TravelType.Gondola;

        if (combined.Contains("propylon"))
            return TravelType.Propylon;

        // For NPCs with non-standard classes that have travel destinations,
        // try to infer travel type from their placement location
        if (npc.HasTravelDestinations && !string.IsNullOrEmpty(id))
        {
            var inferredType = InferTravelTypeFromContext(npc);
            if (inferredType != TravelType.Unknown)
                return inferredType;
        }

        return TravelType.Unknown;
    }

    /// <summary>
    /// Attempts to infer travel type from NPC placement and destination context.
    /// Used for NPCs with non-standard classes (e.g., Monk, Pauper) who provide travel.
    /// </summary>
    private TravelType InferTravelTypeFromContext(TES3Npc npc)
    {
        var id = npc.Id ?? "";

        // Check if NPC is placed in a location that suggests boat travel
        if (_npcPlacements.TryGetValue(id, out var placement))
        {
            var cellName = placement.cellName.ToLowerInvariant();

            // Dock/harbor/port locations suggest boat travel
            if (cellName.Contains("dock") || cellName.Contains("harbor") ||
                cellName.Contains("port") || cellName.Contains("wharf") ||
                cellName.Contains("pier") || cellName.Contains("quay"))
            {
                return TravelType.Boat;
            }

            // Known coastal cities with boat services
            if (cellName.Contains("ebonheart") || cellName.Contains("hla oad") ||
                cellName.Contains("gnaar mok") || cellName.Contains("khuul") ||
                cellName.Contains("tel branora") || cellName.Contains("tel mora") ||
                cellName.Contains("tel aruhn") || cellName.Contains("sadrith mora") ||
                cellName.Contains("dagon fel") || cellName.Contains("molag mar") ||
                cellName.Contains("vivec") || cellName.Contains("holamayan"))
            {
                return TravelType.Boat;
            }
        }

        // Check destination names for boat-related hints
        if (npc.TravelDestinations != null)
        {
            foreach (var dest in npc.TravelDestinations)
            {
                var destCell = (dest.Cell ?? "").ToLowerInvariant();

                if (destCell.Contains("dock") || destCell.Contains("harbor") ||
                    destCell.Contains("holamayan") || destCell.Contains("ebonheart") ||
                    destCell.Contains("tel ") || destCell.Contains("sadrith") ||
                    destCell.Contains("dagon fel"))
                {
                    return TravelType.Boat;
                }
            }
        }

        return TravelType.Unknown;
    }

    /// <summary>
    /// Resolves a position to exterior grid coordinates.
    /// For exterior cells, converts world units directly.
    /// For interior cells, traces through doors to find an exterior exit.
    /// </summary>
    private (double gridX, double gridY, bool isInterior) ResolvePosition(string cellName, double x, double y)
    {
        // Check if this is an exterior cell (by grid format)
        if (cellName.StartsWith("(") || cellName.StartsWith("EXTERIOR:"))
        {
            return (x / UnitsPerCell, y / UnitsPerCell, false);
        }

        // This is an interior cell - try to trace to exterior through doors
        var exteriorPos = TraceToExterior(cellName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        if (exteriorPos != null)
        {
            return (exteriorPos.Value.gridX, exteriorPos.Value.gridY, true);
        }

        // Couldn't find exterior - return NaN
        return (double.NaN, double.NaN, true);
    }

    /// <summary>
    /// Traces from an interior cell through doors until an exterior cell is found.
    /// Uses BFS to find the shortest path to exterior.
    /// </summary>
    private (double gridX, double gridY)? TraceToExterior(string startCellName, HashSet<string> visited)
    {
        var queue = new Queue<string>();
        queue.Enqueue(startCellName);

        while (queue.Count > 0)
        {
            var cellName = queue.Dequeue();

            if (visited.Contains(cellName))
                continue;
            visited.Add(cellName);

            // Get door connections from this cell
            if (!_doorConnections.TryGetValue(cellName, out var connections))
                continue;

            foreach (var (destCell, destX, destY) in connections)
            {
                // Check if destination is exterior
                if (destCell.StartsWith("EXTERIOR:"))
                {
                    // Parse the exterior coordinates from the destination
                    return (destX / UnitsPerCell, destY / UnitsPerCell);
                }

                // Check if the destination cell exists and is exterior
                if (_cells.TryGetValue(destCell, out var cell) && cell.IsExterior)
                {
                    return (destX / UnitsPerCell, destY / UnitsPerCell);
                }

                // It's another interior - add to queue
                if (!visited.Contains(destCell))
                {
                    queue.Enqueue(destCell);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Clears all accumulated data to start fresh.
    /// </summary>
    public void Clear()
    {
        _npcsWithTravel.Clear();
        _npcPlacements.Clear();
        _cells.Clear();
        _doorConnections.Clear();
        _propylonChambers.Clear();
        PluginsProcessed = 0;
    }
}
