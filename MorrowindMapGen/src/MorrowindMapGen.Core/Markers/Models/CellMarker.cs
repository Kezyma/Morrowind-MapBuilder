using System.Text.Json.Serialization;

namespace MorrowindMapGen.Core.Markers.Models;

/// <summary>
/// Represents a map marker for an exterior cell.
/// </summary>
public class CellMarker
{
    /// <summary>
    /// The name of the cell (e.g., "Balmora", "Bitter Coast Region").
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The X coordinate in the cell grid.
    /// </summary>
    [JsonPropertyName("gridX")]
    public int GridX { get; set; }

    /// <summary>
    /// The Y coordinate in the cell grid.
    /// </summary>
    [JsonPropertyName("gridY")]
    public int GridY { get; set; }

    public override string ToString() => $"{Name} ({GridX}, {GridY})";
}
