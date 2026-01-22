using System.Text.Json.Serialization;

namespace MorrowindMapGen.Core.Markers.Models;

/// <summary>
/// Type of fast travel service.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TravelType
{
    SiltStrider,
    Boat,
    Gondola,
    MagesGuild,
    Propylon,
    Unknown
}

/// <summary>
/// Represents a fast travel service location (NPC who provides travel services).
/// </summary>
public class TravelNode
{
    /// <summary>
    /// The unique ID of the NPC providing travel services.
    /// </summary>
    [JsonPropertyName("npcId")]
    public required string NpcId { get; set; }

    /// <summary>
    /// Display name of the NPC.
    /// </summary>
    [JsonPropertyName("npcName")]
    public required string NpcName { get; set; }

    /// <summary>
    /// Name of the cell where the NPC is located.
    /// </summary>
    [JsonPropertyName("cellName")]
    public string? CellName { get; set; }

    /// <summary>
    /// Type of travel service offered.
    /// </summary>
    [JsonPropertyName("type")]
    public TravelType Type { get; set; }

    /// <summary>
    /// X coordinate in grid units.
    /// </summary>
    [JsonPropertyName("gridX")]
    public double GridX { get; set; }

    /// <summary>
    /// Y coordinate in grid units.
    /// </summary>
    [JsonPropertyName("gridY")]
    public double GridY { get; set; }

    /// <summary>
    /// Whether the NPC is placed in an interior cell.
    /// </summary>
    [JsonPropertyName("isInInterior")]
    public bool IsInInterior { get; set; }

    public override string ToString() => $"{NpcName} ({Type}) at ({GridX:F2}, {GridY:F2})";
}

/// <summary>
/// Represents a single fast travel route from one location to another.
/// </summary>
public class TravelRoute
{
    /// <summary>
    /// NPC ID of the travel service provider.
    /// </summary>
    [JsonPropertyName("fromNpcId")]
    public required string FromNpcId { get; set; }

    /// <summary>
    /// Destination cell name.
    /// </summary>
    [JsonPropertyName("toCell")]
    public required string ToCell { get; set; }

    /// <summary>
    /// Type of travel service.
    /// </summary>
    [JsonPropertyName("type")]
    public TravelType Type { get; set; }

    /// <summary>
    /// Starting X coordinate in grid units.
    /// </summary>
    [JsonPropertyName("fromGridX")]
    public double FromGridX { get; set; }

    /// <summary>
    /// Starting Y coordinate in grid units.
    /// </summary>
    [JsonPropertyName("fromGridY")]
    public double FromGridY { get; set; }

    /// <summary>
    /// Destination X coordinate in grid units.
    /// </summary>
    [JsonPropertyName("toGridX")]
    public double ToGridX { get; set; }

    /// <summary>
    /// Destination Y coordinate in grid units.
    /// </summary>
    [JsonPropertyName("toGridY")]
    public double ToGridY { get; set; }

    public override string ToString() => $"{Type}: ({FromGridX:F2}, {FromGridY:F2}) -> {ToCell} ({ToGridX:F2}, {ToGridY:F2})";
}

/// <summary>
/// Collection of all fast travel data.
/// </summary>
public class TravelData
{
    /// <summary>
    /// Fast travel service locations.
    /// </summary>
    [JsonPropertyName("nodes")]
    public List<TravelNode> Nodes { get; set; } = [];

    /// <summary>
    /// Fast travel routes between locations.
    /// </summary>
    [JsonPropertyName("routes")]
    public List<TravelRoute> Routes { get; set; } = [];
}
