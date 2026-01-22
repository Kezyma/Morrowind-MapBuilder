namespace MorrowindMapGen.Core.MapGeneration;

/// <summary>
/// Information about an additional tile layer to include in the map.
/// </summary>
public class LayerInfo
{
    /// <summary>
    /// Display name of the layer (used in UI toggle).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Path to the directory containing (x,y).png tile files.
    /// For built-in layers, this may be empty or null.
    /// </summary>
    public string InputPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether this layer appears above the base map (overlay) or below it (underlay/base).
    /// </summary>
    public bool IsOverlay { get; set; }

    /// <summary>
    /// Whether this layer is enabled by default when the map loads.
    /// </summary>
    public bool EnabledByDefault { get; set; } = true;

    /// <summary>
    /// Display order in the layer control (lower values appear first).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this is a built-in layer (Generated Map, Cell Markers, Door Markers).
    /// Built-in layers cannot be removed by the user.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// The maximum zoom level at which this layer has native tiles.
    /// For layers with smaller source tiles, this will be less than the base map's max zoom.
    /// Calculated during tile processing based on source tile size.
    /// </summary>
    public int MaxNativeZoom { get; set; }

    /// <summary>
    /// Original size of the source tiles in pixels.
    /// Detected from the first tile file in the source directory.
    /// </summary>
    public int SourceTileSize { get; set; }

    /// <summary>
    /// Target size after scaling to nearest power of 2 (16, 32, 64, 128, or 256).
    /// Used for calculating how many source tiles to stitch together.
    /// </summary>
    public int ScaledTileSize { get; set; }

    /// <summary>
    /// Whether this layer has a fallback image for missing tiles.
    /// Set during tile processing.
    /// </summary>
    public bool HasFallback { get; set; }

    public override string ToString() => $"{Name} ({(IsOverlay ? "overlay" : "base")}){(IsBuiltIn ? " [built-in]" : "")}: {InputPath}";
}
