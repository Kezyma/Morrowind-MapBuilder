using System.Text.Json.Serialization;

namespace MorrowindMapGen.Core.Markers.Models;

/// <summary>
/// Collection of all map markers.
/// </summary>
public class MarkerCollection
{
    /// <summary>
    /// Markers for named exterior cells.
    /// </summary>
    [JsonPropertyName("cells")]
    public List<CellMarker> Cells { get; set; } = [];

    /// <summary>
    /// Markers for doors leading to interior cells.
    /// </summary>
    [JsonPropertyName("doors")]
    public List<DoorMarker> Doors { get; set; } = [];

    /// <summary>
    /// Fast travel network data (nodes and routes).
    /// </summary>
    [JsonPropertyName("travel")]
    public TravelData? Travel { get; set; }

    /// <summary>
    /// Total count of all markers.
    /// </summary>
    [JsonIgnore]
    public int TotalCount => Cells.Count + Doors.Count + (Travel?.Nodes.Count ?? 0);
}
