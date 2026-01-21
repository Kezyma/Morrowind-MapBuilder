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
    /// </summary>
    public required string InputPath { get; set; }

    /// <summary>
    /// Whether this layer appears above the base map (overlay) or below it (underlay).
    /// </summary>
    public bool IsOverlay { get; set; }

    public override string ToString() => $"{Name} ({(IsOverlay ? "overlay" : "underlay")}): {InputPath}";
}
