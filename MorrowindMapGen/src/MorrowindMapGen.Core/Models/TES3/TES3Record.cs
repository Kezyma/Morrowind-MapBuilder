using System.Text.Json.Serialization;

namespace MorrowindMapGen.Core.Models.TES3;

/// <summary>
/// Base class for TES3 records from tes3conv JSON output.
/// </summary>
public class TES3Record
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// TES3 Cell record representing an interior or exterior cell.
/// </summary>
public class TES3Cell : TES3Record
{
    /// <summary>
    /// The display name of the cell (e.g., "Balmora").
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Cell-level flags as a string (e.g., may be empty or contain flag info).
    /// </summary>
    [JsonPropertyName("flags")]
    public string? Flags { get; set; }

    /// <summary>
    /// The region this cell belongs to (exterior cells only).
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("data")]
    public TES3CellData? Data { get; set; }

    [JsonPropertyName("references")]
    public List<TES3Reference>? References { get; set; }

    /// <summary>
    /// Gets whether this cell is an interior cell.
    /// Checks if the data flags string contains "IS_INTERIOR".
    /// </summary>
    [JsonIgnore]
    public bool IsInterior => Data?.Flags?.Contains("IS_INTERIOR", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Gets whether this cell is an exterior cell.
    /// </summary>
    [JsonIgnore]
    public bool IsExterior => !IsInterior;
}

/// <summary>
/// Cell data containing flags and grid position.
/// </summary>
public class TES3CellData
{
    /// <summary>
    /// Flags as a string (e.g., "IS_INTERIOR" or empty for exterior).
    /// </summary>
    [JsonPropertyName("flags")]
    public string? Flags { get; set; }

    [JsonPropertyName("grid")]
    public int[]? Grid { get; set; }

    /// <summary>
    /// Gets the X coordinate of the cell grid.
    /// </summary>
    [JsonIgnore]
    public int GridX => Grid?.Length > 0 ? Grid[0] : 0;

    /// <summary>
    /// Gets the Y coordinate of the cell grid.
    /// </summary>
    [JsonIgnore]
    public int GridY => Grid?.Length > 1 ? Grid[1] : 0;
}

/// <summary>
/// Reference to an object placed within a cell.
/// </summary>
public class TES3Reference
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Whether this reference has been deleted by a plugin.
    /// </summary>
    [JsonPropertyName("deleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("mast_index")]
    public int MastIndex { get; set; }

    [JsonPropertyName("refr_index")]
    public int RefrIndex { get; set; }

    /// <summary>
    /// World position of this object [x, y, z].
    /// </summary>
    [JsonPropertyName("translation")]
    public double[]? Translation { get; set; }

    [JsonPropertyName("rotation")]
    public double[]? Rotation { get; set; }

    [JsonPropertyName("scale")]
    public double? Scale { get; set; }

    /// <summary>
    /// Destination for doors/teleport markers.
    /// </summary>
    [JsonPropertyName("destination")]
    public TES3Destination? Destination { get; set; }

    /// <summary>
    /// Gets whether this reference has a destination (is a door or teleport).
    /// </summary>
    [JsonIgnore]
    public bool HasDestination => Destination != null;

    /// <summary>
    /// Gets the X translation (world units).
    /// </summary>
    [JsonIgnore]
    public double TranslationX => Translation?.Length > 0 ? Translation[0] : 0;

    /// <summary>
    /// Gets the Y translation (world units).
    /// </summary>
    [JsonIgnore]
    public double TranslationY => Translation?.Length > 1 ? Translation[1] : 0;

    /// <summary>
    /// Gets the Z translation (world units).
    /// </summary>
    [JsonIgnore]
    public double TranslationZ => Translation?.Length > 2 ? Translation[2] : 0;
}

/// <summary>
/// Destination data for doors and teleport markers.
/// </summary>
public class TES3Destination
{
    /// <summary>
    /// The destination cell name.
    /// </summary>
    [JsonPropertyName("cell")]
    public string? Cell { get; set; }

    [JsonPropertyName("translation")]
    public double[]? Translation { get; set; }

    [JsonPropertyName("rotation")]
    public double[]? Rotation { get; set; }
}
