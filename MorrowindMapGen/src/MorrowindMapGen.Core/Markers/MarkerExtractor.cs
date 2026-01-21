using System.Text.Json;
using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.Markers.Models;
using MorrowindMapGen.Core.Models.TES3;

namespace MorrowindMapGen.Core.Markers;

/// <summary>
/// Extracts map markers from tes3conv JSON output.
/// </summary>
public class MarkerExtractor
{
    private readonly ILogger<MarkerExtractor> _logger;

    /// <summary>
    /// World units per cell (8192 units = 1 cell).
    /// </summary>
    private const double UnitsPerCell = 8192.0;

    public MarkerExtractor(ILogger<MarkerExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts markers from a tes3conv JSON file.
    /// </summary>
    /// <param name="jsonPath">Path to the JSON file.</param>
    /// <param name="minX">Minimum X cell coordinate from map tiles (for normalization).</param>
    /// <param name="maxY">Maximum Y cell coordinate from map tiles (for normalization).</param>
    /// <returns>Collection of extracted markers.</returns>
    public MarkerCollection ExtractFromJson(string jsonPath, int? minX = null, int? maxY = null)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"JSON file not found: {jsonPath}");
        }

        _logger.LogInformation("Extracting markers from {Path}...", Path.GetFileName(jsonPath));

        var jsonContent = File.ReadAllText(jsonPath);

        var collection = new MarkerCollection();
        var cellsByGrid = new Dictionary<(int, int), TES3Cell>();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Parse using JsonDocument to preserve full structure of each record
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Expected JSON array at root");
        }

        // First pass: collect all cells and extract cell markers
        foreach (var element in root.EnumerateArray())
        {
            // Check if this is a Cell record
            if (element.TryGetProperty("type", out var typeProp) &&
                typeProp.GetString()?.Equals("Cell", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Deserialize the full element as TES3Cell
                var cell = JsonSerializer.Deserialize<TES3Cell>(element.GetRawText(), options);

                if (cell?.Data?.Grid != null && cell.Data.Grid.Length == 2)
                {
                    // Check for exterior cells (have a region and are not interior)
                    var isExterior = !string.IsNullOrWhiteSpace(cell.Region) && cell.IsExterior;

                    if (isExterior)
                    {
                        // Extract named exterior cell markers
                        if (!string.IsNullOrWhiteSpace(cell.Name))
                        {
                            collection.Cells.Add(new CellMarker
                            {
                                Name = cell.Name,
                                GridX = cell.Data.GridX,
                                GridY = cell.Data.GridY
                            });
                        }

                        // Store cell for door extraction (all exterior cells, not just named ones)
                        cellsByGrid[(cell.Data.GridX, cell.Data.GridY)] = cell;
                    }
                }
            }
        }

        _logger.LogDebug("Found {Count} named exterior cells", collection.Cells.Count);

        // Second pass: extract door markers from exterior cells
        foreach (var (gridPos, cell) in cellsByGrid)
        {
            if (cell.References == null) continue;

            foreach (var reference in cell.References)
            {
                if (reference.HasDestination &&
                    !string.IsNullOrWhiteSpace(reference.Destination?.Cell))
                {
                    // The door position is the reference's translation (world position)
                    // Convert to grid coordinates
                    var doorGridX = reference.TranslationX / UnitsPerCell;
                    var doorGridY = reference.TranslationY / UnitsPerCell;

                    collection.Doors.Add(new DoorMarker
                    {
                        DestinationCell = reference.Destination.Cell,
                        GridX = doorGridX,
                        GridY = doorGridY
                    });
                }
            }
        }

        _logger.LogDebug("Found {Count} door markers", collection.Doors.Count);

        // Determine normalization offsets
        // Use provided values or compute from the cell data
        var offsetX = minX ?? (cellsByGrid.Count > 0 ? cellsByGrid.Keys.Min(k => k.Item1) : 0);
        var offsetY = maxY ?? (cellsByGrid.Count > 0 ? cellsByGrid.Keys.Max(k => k.Item2) : 0);

        _logger.LogDebug("Normalizing coordinates with offsetX={OffsetX}, offsetY={OffsetY}", offsetX, offsetY);

        // Normalize cell marker coordinates (matching old TileGeneratorService approach)
        foreach (var cell in collection.Cells)
        {
            cell.GridX = cell.GridX - offsetX;
            cell.GridY = cell.GridY - offsetY;  // This will be 0 or negative
        }

        // Normalize door marker coordinates
        foreach (var door in collection.Doors)
        {
            door.GridX = door.GridX - offsetX;
            door.GridY = door.GridY - offsetY;  // This will be 0 or negative
        }

        // Remove duplicate doors (same destination at similar positions)
        collection.Doors = DeduplicateDoors(collection.Doors);

        _logger.LogInformation("Extracted {CellCount} cell markers and {DoorCount} door markers",
            collection.Cells.Count, collection.Doors.Count);

        return collection;
    }

    /// <summary>
    /// Removes duplicate door markers that point to the same destination at similar positions.
    /// </summary>
    private List<DoorMarker> DeduplicateDoors(List<DoorMarker> doors)
    {
        var result = new List<DoorMarker>();
        var seen = new HashSet<string>();

        foreach (var door in doors)
        {
            // Create a key based on destination and rounded position
            var key = $"{door.DestinationCell}_{Math.Round(door.GridX, 1)}_{Math.Round(door.GridY, 1)}";

            if (seen.Add(key))
            {
                result.Add(door);
            }
        }

        if (doors.Count != result.Count)
        {
            _logger.LogDebug("Removed {Count} duplicate door markers", doors.Count - result.Count);
        }

        return result;
    }

    /// <summary>
    /// Saves markers to a JSON file.
    /// </summary>
    /// <param name="markers">The marker collection to save.</param>
    /// <param name="outputPath">Path for the output JSON file.</param>
    public void SaveToJson(MarkerCollection markers, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(markers, options);
        File.WriteAllText(outputPath, json);

        _logger.LogInformation("Saved markers to {Path}", outputPath);
    }
}
