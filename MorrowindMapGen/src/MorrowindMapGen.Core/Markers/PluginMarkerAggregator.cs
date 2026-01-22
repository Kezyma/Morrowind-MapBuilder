using System.Text.Json;
using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.Markers.Models;
using MorrowindMapGen.Core.Models.TES3;

namespace MorrowindMapGen.Core.Markers;

/// <summary>
/// Aggregates markers from multiple plugins processed in load order.
/// Uses a "last write wins" approach where later plugins override earlier ones.
/// This replaces the merge_to_master approach which was causing marker data loss.
/// </summary>
public class PluginMarkerAggregator
{
    private readonly ILogger<PluginMarkerAggregator> _logger;

    /// <summary>
    /// World units per cell (8192 units = 1 cell).
    /// </summary>
    private const double UnitsPerCell = 8192.0;

    /// <summary>
    /// Exterior cells keyed by (GridX, GridY). Later plugins overwrite earlier ones.
    /// </summary>
    private readonly Dictionary<(int, int), CellMarker> _cellsByGrid = new();

    /// <summary>
    /// Door markers keyed by "destination_roundedX_roundedY". Later plugins overwrite earlier ones.
    /// </summary>
    private readonly Dictionary<string, DoorMarker> _doors = new();

    /// <summary>
    /// Count of plugins processed.
    /// </summary>
    public int PluginsProcessed { get; private set; }

    public PluginMarkerAggregator(ILogger<PluginMarkerAggregator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Process a single plugin's JSON file, updating accumulated markers.
    /// Call this for each plugin in load order. Later plugins will override
    /// earlier ones at the same grid coordinates.
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
        _logger.LogDebug("Processing plugin: {Plugin}", pluginName);

        var jsonContent = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        int cellsAdded = 0;
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

            // Process all Cell records
            foreach (var element in root.EnumerateArray())
            {
                if (!element.TryGetProperty("type", out var typeProp))
                    continue;

                var recordType = typeProp.GetString();
                if (!string.Equals(recordType, "Cell", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Deserialize the cell
                var cell = JsonSerializer.Deserialize<TES3Cell>(element.GetRawText(), options);
                if (cell?.Data?.Grid == null || cell.Data.Grid.Length < 2)
                    continue;

                // Only process exterior cells (have a region and are not interior)
                var isExterior = !string.IsNullOrWhiteSpace(cell.Region) && cell.IsExterior;
                if (!isExterior)
                    continue;

                var gridX = cell.Data.GridX;
                var gridY = cell.Data.GridY;

                // Add/update cell marker if the cell has a name
                if (!string.IsNullOrWhiteSpace(cell.Name))
                {
                    _cellsByGrid[(gridX, gridY)] = new CellMarker
                    {
                        Name = cell.Name,
                        GridX = gridX,
                        GridY = gridY
                    };
                    cellsAdded++;
                }

                // Process door references
                if (cell.References != null)
                {
                    foreach (var reference in cell.References)
                    {
                        if (!reference.HasDestination ||
                            string.IsNullOrWhiteSpace(reference.Destination?.Cell))
                            continue;

                        // Calculate door position in grid coordinates
                        var doorGridX = reference.TranslationX / UnitsPerCell;
                        var doorGridY = reference.TranslationY / UnitsPerCell;

                        // Create a key based on destination and rounded position
                        // This deduplicates doors that lead to the same place at similar positions
                        var key = $"{reference.Destination.Cell}_{Math.Round(doorGridX, 1)}_{Math.Round(doorGridY, 1)}";

                        _doors[key] = new DoorMarker
                        {
                            DestinationCell = reference.Destination.Cell,
                            GridX = doorGridX,
                            GridY = doorGridY
                        };
                        doorsAdded++;
                    }
                }
            }

            PluginsProcessed++;
            _logger.LogDebug("Plugin {Plugin}: {Cells} cells, {Doors} doors added/updated",
                pluginName, cellsAdded, doorsAdded);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Failed to parse JSON from {Path}: {Message}", jsonPath, ex.Message);
        }
    }

    /// <summary>
    /// Get the aggregated marker collection after all plugins have been processed.
    /// </summary>
    /// <returns>Collection of all accumulated markers.</returns>
    public MarkerCollection GetMarkers()
    {
        _logger.LogInformation(
            "Aggregated markers from {PluginCount} plugins: {CellCount} cells, {DoorCount} doors",
            PluginsProcessed, _cellsByGrid.Count, _doors.Count);

        return new MarkerCollection
        {
            Cells = _cellsByGrid.Values.ToList(),
            Doors = _doors.Values.ToList()
        };
    }

    /// <summary>
    /// Clears all accumulated markers to start fresh.
    /// </summary>
    public void Clear()
    {
        _cellsByGrid.Clear();
        _doors.Clear();
        PluginsProcessed = 0;
    }
}
