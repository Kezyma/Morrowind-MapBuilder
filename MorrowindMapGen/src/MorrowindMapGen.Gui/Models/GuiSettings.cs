using System.Text.Json.Serialization;
using MorrowindMapGen.Core.MapGeneration;

namespace MorrowindMapGen.Gui.Models;

/// <summary>
/// Game type for configuration.
/// </summary>
public enum GameType
{
    OpenMW,
    Morrowind
}

/// <summary>
/// Application settings that are persisted to disk.
/// </summary>
public class GuiSettings
{
    /// <summary>
    /// Settings format version for future migration support.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Selected game type.
    /// </summary>
    public GameType GameType { get; set; } = GameType.OpenMW;

    /// <summary>
    /// Path to OpenMW configuration file.
    /// </summary>
    public string? OpenMWConfigPath { get; set; }

    /// <summary>
    /// Path to Morrowind.ini configuration file.
    /// </summary>
    public string? MorrowindIniPath { get; set; }

    /// <summary>
    /// Output directory for generated files.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Whether to run the OpenMW Map Generator tool.
    /// </summary>
    public bool GenerateTiles { get; set; } = true;

    /// <summary>
    /// Whether to extract and include map markers.
    /// </summary>
    public bool GenerateMarkers { get; set; } = true;

    /// <summary>
    /// Whether to generate a full-size stitched map image.
    /// </summary>
    public bool GenerateBigMap { get; set; }

    /// <summary>
    /// Whether to generate the web map (tiles and HTML viewer).
    /// </summary>
    public bool GenerateWebMap { get; set; } = true;

    /// <summary>
    /// Whether to use 512px tiles instead of 256px.
    /// </summary>
    public bool Use512pxTiles { get; set; }

    /// <summary>
    /// Output format for tiles.
    /// </summary>
    public TileOutputFormat OutputFormat { get; set; } = TileOutputFormat.WebP;

    /// <summary>
    /// Whether cell markers are enabled by default.
    /// </summary>
    public bool CellMarkersEnabled { get; set; } = true;

    /// <summary>
    /// Whether door markers are enabled by default.
    /// </summary>
    public bool DoorMarkersEnabled { get; set; }

    /// <summary>
    /// Whether fast travel markers are enabled by default.
    /// </summary>
    public bool FastTravelEnabled { get; set; }

    /// <summary>
    /// Whether the generated map layer is an overlay instead of base layer.
    /// </summary>
    public bool GeneratedMapIsOverlay { get; set; }

    /// <summary>
    /// Custom layers configured by the user.
    /// </summary>
    public List<GuiLayerInfo> CustomLayers { get; set; } = new();

    /// <summary>
    /// Gets the active configuration path based on game type.
    /// </summary>
    [JsonIgnore]
    public string? ActiveConfigPath => GameType switch
    {
        GameType.OpenMW => OpenMWConfigPath,
        GameType.Morrowind => MorrowindIniPath,
        _ => null
    };

    /// <summary>
    /// Gets all layers including built-in layers.
    /// </summary>
    public List<GuiLayerInfo> GetAllLayers()
    {
        var generatedMapLayer = GuiLayerInfo.CreateBuiltInLayer("Generated Map", GeneratedMapIsOverlay, 0);

        var cellsLayer = GuiLayerInfo.CreateBuiltInLayer("Cells", true, 100);
        cellsLayer.EnabledByDefault = CellMarkersEnabled;

        var doorsLayer = GuiLayerInfo.CreateBuiltInLayer("Doors", true, 101);
        doorsLayer.EnabledByDefault = DoorMarkersEnabled;

        var fastTravelLayer = GuiLayerInfo.CreateBuiltInLayer("Fast Travel", true, 102);
        fastTravelLayer.EnabledByDefault = FastTravelEnabled;

        var layers = new List<GuiLayerInfo>
        {
            generatedMapLayer,
            cellsLayer,
            doorsLayer,
            fastTravelLayer
        };

        // Add custom layers with sort order between base and markers
        var sortedCustom = CustomLayers.OrderBy(l => l.SortOrder).ToList();
        for (int i = 0; i < sortedCustom.Count; i++)
        {
            sortedCustom[i].SortOrder = i + 1;
        }
        layers.AddRange(sortedCustom);

        return layers.OrderBy(l => l.SortOrder).ToList();
    }

    /// <summary>
    /// Gets the count of base layers (non-overlay tile layers).
    /// </summary>
    public int GetBaseLayerCount()
    {
        var count = GeneratedMapIsOverlay ? 0 : 1;
        count += CustomLayers.Count(l => !l.IsOverlay);
        return count;
    }

    /// <summary>
    /// Creates a default settings instance.
    /// </summary>
    public static GuiSettings CreateDefault()
    {
        return new GuiSettings();
    }
}
