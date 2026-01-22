using MorrowindMapGen.Core.MapGeneration;

namespace MorrowindMapGen.Gui.Models;

/// <summary>
/// Extended layer information for GUI display and editing.
/// </summary>
public class GuiLayerInfo
{
    /// <summary>
    /// Unique identifier for the layer.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name of the layer.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is an overlay layer (true) or base layer (false).
    /// </summary>
    public bool IsOverlay { get; set; }

    /// <summary>
    /// Path to the source tile folder.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Whether this layer is enabled by default in the viewer.
    /// </summary>
    public bool EnabledByDefault { get; set; } = true;

    /// <summary>
    /// Sort order for display in the layer control.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this is a built-in layer that cannot be removed.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Creates a new custom layer.
    /// </summary>
    public static GuiLayerInfo CreateCustomLayer(string name, bool isOverlay, string? sourcePath = null)
    {
        return new GuiLayerInfo
        {
            Name = name,
            IsOverlay = isOverlay,
            SourcePath = sourcePath,
            IsBuiltIn = false
        };
    }

    /// <summary>
    /// Creates a built-in layer that cannot be removed.
    /// </summary>
    public static GuiLayerInfo CreateBuiltInLayer(string name, bool isOverlay, int sortOrder)
    {
        return new GuiLayerInfo
        {
            Name = name,
            IsOverlay = isOverlay,
            IsBuiltIn = true,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Converts to a LayerInfo for use with the Core library.
    /// </summary>
    public LayerInfo ToLayerInfo()
    {
        return new LayerInfo
        {
            Name = Name,
            IsOverlay = IsOverlay,
            InputPath = SourcePath ?? string.Empty,
            EnabledByDefault = EnabledByDefault,
            SortOrder = SortOrder,
            IsBuiltIn = IsBuiltIn
        };
    }

    /// <summary>
    /// Creates a copy of this layer info.
    /// </summary>
    public GuiLayerInfo Clone()
    {
        return new GuiLayerInfo
        {
            Id = Guid.NewGuid(),
            Name = Name,
            IsOverlay = IsOverlay,
            SourcePath = SourcePath,
            EnabledByDefault = EnabledByDefault,
            SortOrder = SortOrder,
            IsBuiltIn = IsBuiltIn
        };
    }
}
