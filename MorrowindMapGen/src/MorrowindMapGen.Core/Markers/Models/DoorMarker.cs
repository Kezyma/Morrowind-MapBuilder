using System.Text.Json.Serialization;

namespace MorrowindMapGen.Core.Markers.Models;

/// <summary>
/// Represents a map marker for a door leading to an interior cell.
/// </summary>
public class DoorMarker
{
    /// <summary>
    /// The name of the destination interior cell.
    /// </summary>
    [JsonPropertyName("destination")]
    public required string DestinationCell { get; set; }

    /// <summary>
    /// The X coordinate in the cell grid (can be fractional for precise positioning).
    /// </summary>
    [JsonPropertyName("gridX")]
    public double GridX { get; set; }

    /// <summary>
    /// The Y coordinate in the cell grid (can be fractional for precise positioning).
    /// </summary>
    [JsonPropertyName("gridY")]
    public double GridY { get; set; }

    public override string ToString() => $"{DestinationCell} at ({GridX:F2}, {GridY:F2})";
}
