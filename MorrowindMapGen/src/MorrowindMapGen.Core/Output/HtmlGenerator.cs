using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.MapGeneration;
using MorrowindMapGen.Core.Markers.Models;

namespace MorrowindMapGen.Core.Output;

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
    public void GenerateViewer(string outputPath, MapMetadata metadata, List<LayerInfo>? layers = null, MarkerCollection? markers = null)
    {
        _logger.LogInformation("Generating HTML viewer...");

        var cellsJson = "[]";
        var doorsJson = "[]";

        if (markers != null)
        {
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            cellsJson = JsonSerializer.Serialize(markers.Cells, jsonOptions);
            doorsJson = JsonSerializer.Serialize(markers.Doors, jsonOptions);
        }

        var html = GenerateHtml(metadata, layers ?? new List<LayerInfo>(), cellsJson, doorsJson);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, html);

        _logger.LogInformation("HTML viewer generated: {Path}", outputPath);
    }

    private string GenerateHtml(MapMetadata metadata, List<LayerInfo> layers, string cellsJson, string doorsJson)
    {
        // Calculate extent in pixels (matching old TileGeneratorService approach)
        var extentX = metadata.WidthInTiles * metadata.TileSize;
        var extentY = metadata.HeightInTiles * metadata.TileSize;
        var hasLayers = layers.Count > 0;
        var baseTilePath = hasLayers ? "base/" : "";

        var sb = new StringBuilder();

        sb.AppendLine(@"<!DOCTYPE html>
<html>
    <head>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
        <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css' />
        <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
        <style>
            .leaflet-tooltip { background-color: black; border: 1px solid #caa560; border-radius: 0; color: #caa560; }
            .leaflet-tooltip::before { display: none; }
            .layer-toggle-btn { cursor: pointer; user-select: none; }
            .layer-toggle-btn.disabled { opacity: 0.5; }
        </style>
    </head>
    <body style='margin:0;padding:0;height:100vh;width:100vw;'>
        <div id='map' style='height:100%;width:100%;background-color:#1e1c18;'></div>
        <script>");

        // Embed marker data
        sb.AppendLine($"var mapCells = {cellsJson};");
        sb.AppendLine($"var mapDoors = {doorsJson};");
        sb.AppendLine();

        // Generate layer definitions
        var underlayers = layers.Where(l => !l.IsOverlay).ToList();
        var overlayers = layers.Where(l => l.IsOverlay).ToList();

        var layerDefinitions = new StringBuilder();
        foreach (var layer in underlayers)
        {
            layerDefinitions.AppendLine($@"
underlayers.push({{
    name: '{layer.Name}',
    layer: L.tileLayer('{layer.Name}/{{z}}/{{x}}/{{y}}.png', {{
        minZoom: mapMinZoom,
        maxNativeZoom: mapMaxZoom,
        maxZoom: mapMaxZoom * 2,
        noWrap: true,
        errorTileUrl: '{layer.Name}/fallback.png'
    }}),
    visible: true
}});");
        }
        foreach (var layer in overlayers)
        {
            layerDefinitions.AppendLine($@"
overlayers.push({{
    name: '{layer.Name}',
    layer: L.tileLayer('{layer.Name}/{{z}}/{{x}}/{{y}}.png', {{
        minZoom: mapMinZoom,
        maxNativeZoom: mapMaxZoom,
        maxZoom: mapMaxZoom * 2,
        noWrap: true,
        errorTileUrl: '{layer.Name}/fallback.png'
    }}),
    visible: true
}});");
        }

        // Generate layer toggle buttons
        var layerToggleButtons = new StringBuilder();
        for (int i = 0; i < underlayers.Count; i++)
        {
            var layer = underlayers[i];
            layerToggleButtons.AppendLine($@"
        var ulBtn{i} = L.DomUtil.create('div', 'leaflet-bar leaflet-control layer-toggle-btn', container);
        ulBtn{i}.innerHTML = '{layer.Name}';
        ulBtn{i}.style.backgroundColor = 'white';
        ulBtn{i}.style.padding = '5px 10px';
        ulBtn{i}.style.marginBottom = '2px';
        ulBtn{i}.style.fontSize = '12px';
        ulBtn{i}.onclick = function() {{
            var ul = underlayers[{i}];
            if (ul.visible) {{
                map.removeLayer(ul.layer);
                ul.visible = false;
                ulBtn{i}.classList.add('disabled');
            }} else {{
                ul.layer.addTo(map);
                ul.layer.bringToBack();
                ul.visible = true;
                ulBtn{i}.classList.remove('disabled');
            }}
        }};");
        }
        for (int i = 0; i < overlayers.Count; i++)
        {
            var layer = overlayers[i];
            layerToggleButtons.AppendLine($@"
        var olBtn{i} = L.DomUtil.create('div', 'leaflet-bar leaflet-control layer-toggle-btn', container);
        olBtn{i}.innerHTML = '{layer.Name}';
        olBtn{i}.style.backgroundColor = 'white';
        olBtn{i}.style.padding = '5px 10px';
        olBtn{i}.style.marginBottom = '2px';
        olBtn{i}.style.fontSize = '12px';
        olBtn{i}.onclick = function() {{
            var ol = overlayers[{i}];
            if (ol.visible) {{
                map.removeLayer(ol.layer);
                ol.visible = false;
                olBtn{i}.classList.add('disabled');
            }} else {{
                ol.layer.addTo(map);
                ol.layer.bringToFront();
                ol.visible = true;
                olBtn{i}.classList.remove('disabled');
            }}
        }};");
        }

        // Map configuration - using extents that match the old working code
        sb.AppendLine($@"
var markerToggleOn = true;
var map = null;
var underlayers = [];
var overlayers = [];

// Map configuration
var mapExtent = [0, -{extentY}, {extentX}, 0];
var mapMinZoom = 0;
var mapMaxZoom = {metadata.MaxZoom};
var mapMinResolution = Math.pow(2, mapMaxZoom);

// Configure CRS with proper transformation
var crs = L.CRS.Simple;
crs.transformation = new L.Transformation(1, -mapExtent[0], -1, mapExtent[3]);
crs.scale = function(zoom) {{ return Math.pow(2, zoom) / mapMinResolution; }};
crs.zoom = function(scale) {{ return Math.log(scale * mapMinResolution) / Math.LN2; }};

// Create map
map = new L.Map('map', {{
    maxZoom: mapMaxZoom * 2,
    minZoom: mapMinZoom,
    crs: crs,
    attributionControl: false
}});

// Initialize layer arrays
{layerDefinitions}

// Add underlayers first (bottom of stack)
underlayers.forEach(function(l) {{ l.layer.addTo(map); }});

// Add base tile layer
var baseLayer = L.tileLayer('{baseTilePath}{{z}}/{{x}}/{{y}}.png', {{
    minZoom: mapMinZoom,
    maxNativeZoom: mapMaxZoom,
    maxZoom: mapMaxZoom * 2,
    noWrap: true,
    tms: false,
    errorTileUrl: '{baseTilePath}fallback.png'
}}).addTo(map);

// Add overlayers on top
overlayers.forEach(function(l) {{ l.layer.addTo(map); }});

// Fit bounds
map.fitBounds([
    crs.unproject(L.point(mapExtent[2], mapExtent[3])),
    crs.unproject(L.point(mapExtent[0], mapExtent[1]))
]);

// Marker icon
var mwIcon = L.icon({{
    iconUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAMCAYAAABWdVznAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAO0lEQVQokWP8//8/AymAhYGBgWHb/ByidHklTmFkIsl4mA1IgBGHOrgLSLZhOGhADyWC8UGyDYykJg0AvqIMFsgeioQAAAAASUVORK5CYII=',
    shadowUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAMCAYAAABWdVznAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAGElEQVQokWNkYGD4z0ACYCJF8aiGkaQBAGOyARfDSz7hAAAAAElFTkSuQmCC',
    iconSize: [12, 12],
    shadowSize: [12, 12]
}});

var doorMarkers = new L.FeatureGroup();
var cellMarkers = new L.FeatureGroup();

try {{
    // Add cell markers (using normalized grid coordinates)
    if (mapCells != null) {{
        for (var ix in mapCells) {{
            // Grid coordinates are already normalized (0-based from minX/maxY)
            // Y is inverted in the tile generation, so use gridY directly
            L.marker([((mapCells[ix].gridY) * 256) - 128, ((mapCells[ix].gridX) * 256) + 128], {{icon: mwIcon}})
                .addTo(cellMarkers)
                .bindTooltip(mapCells[ix].name);
        }}
    }}

    // Add door markers
    if (mapDoors != null) {{
        for (var ix in mapDoors) {{
            L.marker([((mapDoors[ix].gridY) * 256) - 256, ((mapDoors[ix].gridX) * 256)], {{icon: mwIcon}})
                .addTo(doorMarkers)
                .bindTooltip(mapDoors[ix].destination);
        }}
    }}

    // Add appropriate markers to the map for the current zoom level
    if (map.getZoom() >= mapMaxZoom) {{
        doorMarkers.addTo(map);
    }} else {{
        cellMarkers.addTo(map);
    }}

    // Toggle between markers when zoom level changes
    map.on('zoomend', function() {{
        if (markerToggleOn) {{
            if (map.getZoom() >= mapMaxZoom) {{
                map.addLayer(doorMarkers);
                map.removeLayer(cellMarkers);
            }} else {{
                map.addLayer(cellMarkers);
                map.removeLayer(doorMarkers);
            }}
        }}
    }});

    // Marker toggle control
    L.Control.MarkerToggle = L.Control.extend({{
        options: {{ position: 'topleft' }},
        onAdd: function(map) {{
            var container = L.DomUtil.create('div', 'leaflet-bar leaflet-control leaflet-control-custom');
            container.style.fontSize = '1rem';
            container.innerHTML = '<i class=""fa-solid fa-location-dot""></i>';
            container.style.backgroundColor = 'white';
            container.style.width = '30px';
            container.style.height = '30px';
            container.style.lineHeight = '30px';
            container.style.textAlign = 'center';
            container.style.cursor = 'pointer';
            container.onclick = function() {{
                if (markerToggleOn == true) {{
                    map.removeLayer(cellMarkers);
                    map.removeLayer(doorMarkers);
                    markerToggleOn = false;
                }} else if (map.getZoom() >= mapMaxZoom) {{
                    map.addLayer(doorMarkers);
                    markerToggleOn = true;
                }} else {{
                    map.addLayer(cellMarkers);
                    markerToggleOn = true;
                }}
            }};
            return container;
        }}
    }});
    new L.Control.MarkerToggle({{ position: 'topleft' }}).addTo(map);
{(hasLayers ? $@"
    // Layer toggle control
    L.Control.LayerToggle = L.Control.extend({{
        options: {{ position: 'topright' }},
        onAdd: function(map) {{
            var container = L.DomUtil.create('div', 'leaflet-control');
{layerToggleButtons}
            return container;
        }}
    }});
    new L.Control.LayerToggle().addTo(map);
" : "")}
}} catch (e) {{
    console.error('Error setting up markers:', e);
}}
");

        sb.AppendLine(@"        </script>
    </body>
</html>");

        return sb.ToString();
    }
}
