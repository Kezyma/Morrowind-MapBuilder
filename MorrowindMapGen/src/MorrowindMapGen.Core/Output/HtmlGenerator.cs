using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.MapGeneration;
using MorrowindMapGen.Core.Markers.Models;

namespace MorrowindMapGen.Core.Output;

/// <summary>
/// Options for HTML generation.
/// </summary>
public class HtmlGeneratorOptions
{
    /// <summary>
    /// File extension for tile images (e.g., ".webp" or ".png").
    /// </summary>
    public string TileExtension { get; set; } = ".webp";

    /// <summary>
    /// Whether cell markers are enabled by default.
    /// </summary>
    public bool CellMarkersEnabled { get; set; } = true;

    /// <summary>
    /// Whether door markers are enabled by default.
    /// </summary>
    public bool DoorMarkersEnabled { get; set; } = false;

    /// <summary>
    /// Whether the generated map layer is an overlay instead of a base layer.
    /// When true, there must be at least one other base layer.
    /// </summary>
    public bool GeneratedMapIsOverlay { get; set; } = false;

    /// <summary>
    /// Whether the generated/base map has a fallback image.
    /// </summary>
    public bool BaseMapHasFallback { get; set; } = false;
}

/// <summary>
/// Generates the HTML viewer with Leaflet.js integration.
/// </summary>
public class HtmlGenerator
{
    private readonly ILogger<HtmlGenerator> _logger;

    public HtmlGenerator(ILogger<HtmlGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates the HTML viewer file.
    /// </summary>
    /// <param name="outputPath">Path to write the HTML file.</param>
    /// <param name="metadata">Map metadata.</param>
    /// <param name="layers">Optional list of additional layers.</param>
    /// <param name="markers">Optional marker collection.</param>
    /// <param name="options">Optional generation options.</param>
    public void GenerateViewer(
        string outputPath,
        MapMetadata metadata,
        List<LayerInfo>? layers = null,
        MarkerCollection? markers = null,
        HtmlGeneratorOptions? options = null)
    {
        options ??= new HtmlGeneratorOptions();
        _logger.LogInformation("Generating HTML viewer...");

        var cellsJson = "[]";
        var doorsJson = "[]";

        if (markers != null)
        {
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            cellsJson = JsonSerializer.Serialize(markers.Cells, jsonOptions);
            doorsJson = JsonSerializer.Serialize(markers.Doors, jsonOptions);
        }

        var html = GenerateHtml(metadata, layers ?? new List<LayerInfo>(), cellsJson, doorsJson, options);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, html);

        _logger.LogInformation("HTML viewer generated: {Path}", outputPath);
    }

    private string GenerateHtml(MapMetadata metadata, List<LayerInfo> layers, string cellsJson, string doorsJson, HtmlGeneratorOptions options)
    {
        // Use raw game grid bounds
        var minX = metadata.MinX;
        var maxX = metadata.MaxX;
        var minY = metadata.MinY;
        var maxY = metadata.MaxY;
        var tileSize = metadata.TileSize;
        var maxZoom = metadata.MaxZoom;
        var hasLayers = layers.Count > 0;
        var baseTilePath = hasLayers ? "base/" : "";
        var ext = options.TileExtension.TrimStart('.');

        var sb = new StringBuilder();

        sb.AppendLine(@"<!DOCTYPE html>
<html>
    <head>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
        <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
        <style>
            .leaflet-tooltip { background-color: black; border: 1px solid #caa560; border-radius: 0; color: #caa560; }
            .leaflet-tooltip::before { display: none; }
        </style>
    </head>
    <body style='margin:0;padding:0;height:100vh;width:100vw;'>
        <div id='map' style='height:100%;width:100%;background-color:#1e1c18;'></div>
        <script>");

        // Embed marker data
        sb.AppendLine($"var mapCells = {cellsJson};");
        sb.AppendLine($"var mapDoors = {doorsJson};");
        sb.AppendLine();

        // Generate base layer definitions (underlayers)
        var baseLayers = layers.Where(l => !l.IsOverlay).ToList();
        var overlayLayers = layers.Where(l => l.IsOverlay).ToList();

        var baseLayerDefs = new StringBuilder();
        var overlayLayerDefs = new StringBuilder();

        // Generated map layer definition (with fallback for missing tiles if available)
        var baseErrorTile = options.BaseMapHasFallback ? $",\n        errorTileUrl: '{baseTilePath}fallback.{ext}'" : "";
        var generatedMapLayerDef = $@"
    ""Generated Map"": L.tileLayer('{baseTilePath}{{z}}/{{x}}/{{y}}.{ext}', {{
        minZoom: mapMinZoom,
        maxNativeZoom: mapMaxZoom,
        maxZoom: mapMaxZoom * 2,
        noWrap: true,
        tileSize: tileSize{baseErrorTile}
    }}),";

        // Add generated map to base or overlay depending on option
        if (options.GeneratedMapIsOverlay)
        {
            overlayLayerDefs.AppendLine(generatedMapLayerDef);
        }
        else
        {
            baseLayerDefs.AppendLine(generatedMapLayerDef);
        }

        // Add other base layers
        foreach (var layer in baseLayers)
        {
            var layerMaxZoom = layer.MaxNativeZoom > 0 ? layer.MaxNativeZoom : maxZoom;
            var layerErrorTile = layer.HasFallback ? $",\n        errorTileUrl: '{layer.Name}/fallback.{ext}'" : "";
            baseLayerDefs.AppendLine($@"
    ""{EscapeJs(layer.Name)}"": L.tileLayer('{layer.Name}/{{z}}/{{x}}/{{y}}.{ext}', {{
        minZoom: mapMinZoom,
        maxNativeZoom: {layerMaxZoom},
        maxZoom: mapMaxZoom * 2,
        noWrap: true,
        tileSize: tileSize{layerErrorTile}
    }}),");
        }

        // Add overlay tile layers
        foreach (var layer in overlayLayers)
        {
            var layerMaxZoom = layer.MaxNativeZoom > 0 ? layer.MaxNativeZoom : maxZoom;
            var layerErrorTile = layer.HasFallback ? $",\n        errorTileUrl: '{layer.Name}/fallback.{ext}'" : "";
            overlayLayerDefs.AppendLine($@"
    ""{EscapeJs(layer.Name)}"": L.tileLayer('{layer.Name}/{{z}}/{{x}}/{{y}}.{ext}', {{
        minZoom: mapMinZoom,
        maxNativeZoom: {layerMaxZoom},
        maxZoom: mapMaxZoom * 2,
        noWrap: true,
        tileSize: tileSize{layerErrorTile}
    }}),");
        }

        // Map configuration
        sb.AppendLine($@"
// Map configuration - tiles stored with normalized web coordinates
// X: offset by minX (starts at 0)
// Y: flipped (Y=0 at top, increases southward)
var gridMinX = {minX};
var gridMaxX = {maxX};
var gridMinY = {minY};
var gridMaxY = {maxY};
var tileSize = {tileSize};
var cellSize = 256; // Each game cell is always 256px, regardless of output tile size
var mapMinZoom = 0;
var mapMaxZoom = {maxZoom};
var mapMinResolution = Math.pow(2, mapMaxZoom);

// Map dimensions in cells
var mapWidthCells = gridMaxX - gridMinX + 1;
var mapHeightCells = gridMaxY - gridMinY + 1;

// Extent in pixels (based on cell size, not tile size)
var mapExtent = [0, 0, mapWidthCells * cellSize, mapHeightCells * cellSize];

// Configure CRS - simple pixel coordinates
// Tiles are stored with Y=0 at top (web-standard), so we need transformation
// that doesn't negate Y (default L.CRS.Simple negates Y)
var crs = L.CRS.Simple;
crs.transformation = new L.Transformation(1, 0, 1, 0);
crs.scale = function(zoom) {{ return Math.pow(2, zoom) / mapMinResolution; }};
crs.zoom = function(scale) {{ return Math.log(scale * mapMinResolution) / Math.LN2; }};

// Create map
var map = new L.Map('map', {{
    maxZoom: mapMaxZoom * 2,
    minZoom: mapMinZoom,
    crs: crs,
    attributionControl: false
}});

// Marker icon
var mwIcon = L.icon({{
    iconUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAMCAYAAABWdVznAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAO0lEQVQokWP8//8/AymAhYGBgWHb/ByidHklTmFkIsl4mA1IgBGHOrgLSLZhOGhADyWC8UGyDYykJg0AvqIMFsgeioQAAAAASUVORK5CYII=',
    shadowUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAMCAYAAABWdVznAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAGElEQVQokWNkYGD4z0ACYCJF8aiGkaQBAGOyARfDSz7hAAAAAElFTkSuQmCC',
    iconSize: [12, 12],
    shadowSize: [12, 12]
}});

// Create marker feature groups
var cellMarkers = new L.FeatureGroup();
var doorMarkers = new L.FeatureGroup();

// Helper to convert game coordinates to normalized pixel coordinates
// Game coords: X increases east, Y increases north
// Normalized: X offset by minX, Y flipped (north at top)
// Uses cellSize (256px) not tileSize, since each game cell is always 256px
function gameToPixel(gameX, gameY) {{
    var normX = (gameX - gridMinX) * cellSize;
    var normY = (gridMaxY + 1 - gameY) * cellSize;
    return [normY, normX]; // Leaflet uses [lat, lng] = [y, x]
}}

// Add cell markers (convert from game grid to normalized pixel coordinates)
// Add 0.5 to center marker within the cell
if (mapCells != null) {{
    for (var ix in mapCells) {{
        var pos = gameToPixel(mapCells[ix].gridX + 0.5, mapCells[ix].gridY + 0.5);
        L.marker(pos, {{icon: mwIcon}})
            .addTo(cellMarkers)
            .bindTooltip(mapCells[ix].name);
    }}
}}

// Add door markers (convert from game grid with sub-cell precision)
if (mapDoors != null) {{
    for (var ix in mapDoors) {{
        var pos = gameToPixel(mapDoors[ix].gridX, mapDoors[ix].gridY);
        L.marker(pos, {{icon: mwIcon}})
            .addTo(doorMarkers)
            .bindTooltip(mapDoors[ix].destination);
    }}
}}

// Define base layers
var baseLayers = {{
{baseLayerDefs}
}};

// Define overlay layers
var overlayLayers = {{
{overlayLayerDefs}
    ""Cells"": cellMarkers,
    ""Doors"": doorMarkers
}};

// Add the default base layer to the map");

        // Determine which base layer to show by default
        if (!options.GeneratedMapIsOverlay)
        {
            sb.AppendLine(@"baseLayers[""Generated Map""].addTo(map);");
        }
        else if (baseLayers.Count > 0)
        {
            // Use the first custom base layer
            sb.AppendLine($@"baseLayers[""{EscapeJs(baseLayers[0].Name)}""].addTo(map);");
        }

        // If Generated Map is an overlay and enabled by default, add it
        if (options.GeneratedMapIsOverlay)
        {
            sb.AppendLine(@"overlayLayers[""Generated Map""].addTo(map);");
        }

        // Add enabled-by-default overlays
        foreach (var layer in overlayLayers.Where(l => l.EnabledByDefault))
        {
            sb.AppendLine($@"overlayLayers[""{EscapeJs(layer.Name)}""].addTo(map);");
        }

        // Add markers based on options
        if (options.CellMarkersEnabled)
        {
            sb.AppendLine(@"overlayLayers[""Cells""].addTo(map);");
        }
        if (options.DoorMarkersEnabled)
        {
            sb.AppendLine(@"overlayLayers[""Doors""].addTo(map);");
        }

        sb.AppendLine($@"
// Add layer control
L.control.layers(baseLayers, overlayLayers).addTo(map);

// Fit bounds - use normalized pixel coordinates
// mapExtent = [0, 0, width, height] in pixels
map.fitBounds([
    [0, 0],
    [mapExtent[3], mapExtent[2]]
]);
");

        sb.AppendLine(@"        </script>
    </body>
</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a string for use in JavaScript.
    /// </summary>
    private static string EscapeJs(string s)
    {
        return s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"");
    }
}
