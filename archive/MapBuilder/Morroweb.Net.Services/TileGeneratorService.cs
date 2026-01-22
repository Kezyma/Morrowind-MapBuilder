using Microsoft.EntityFrameworkCore.Diagnostics;
using Morroweb.Net.Data.Models.TES3;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using nQuant;

namespace Morroweb.Net.Services
{
    public class TileGeneratorService
    {
        public TileGeneratorService(EspExtractorService espExtractor) 
        { 
            _espExtractor = espExtractor;
        }
        private readonly EspExtractorService _espExtractor;

        

        private Regex _coordRegex = new("\\((?<x>[^,]*),(?<y>[^\\)]*)\\)");

        public Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public void StitchTiles(string inputPath, string outputPath, bool quantise = false, int resolution = 256)
        {
            // Get the current generated Morrowind tiles and work out the boundaries of the map.
            var files = Directory.GetFiles(inputPath);
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            foreach (var file in files)
            {
                var fn = file.Split("(")[1].Split(")")[0].Split(",");
                var x = int.Parse(fn[0]);
                var y = int.Parse(fn[1]);
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            var width = maxX - minX;
            var height = maxY - minY;
            var maxSize = 23170;
            var scale = 1.0;
            if ((height * resolution) > maxSize || (width * resolution) > maxSize)
            {
                var largest = (height > width) ? height : width;
                scale = maxSize / (double)(largest * resolution);
            }
            var tileSize = (int)Math.Floor(resolution * scale);

            var tileMatches = files.ToDictionary(x => x, x =>
            {
                var m = _coordRegex.Match(x);
                return new[] { int.Parse(m.Groups["x"].Value), int.Parse(m.Groups["y"].Value) };
            });
            
            //Image img = Image.FromFile
            Bitmap bigMap;
            try
            {
                bigMap = new Bitmap(width * tileSize, height * tileSize, PixelFormat.Format32bppArgb);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Generated image would be too large, aborting.");
                return;
            }
            bigMap.MakeTransparent();
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (tileMatches.Any(t => t.Value[0] == x && t.Value[1] == y))
                    {
                        var tile = tileMatches.FirstOrDefault(t => t.Value[0] == x && t.Value[1] == y);
                        var tileMap = (Bitmap)Bitmap.FromFile(tile.Key);
                        if (tileSize != resolution)
                        {
                            tileMap = ResizeImage(tileMap, tileSize, tileSize);
                        }
                        using (Graphics g = Graphics.FromImage(bigMap))
                        {
                            if (quantise)
                            {
                                var newTileMap = new Bitmap(tileSize, tileSize, PixelFormat.Format32bppArgb);
                                using Graphics qg = Graphics.FromImage(newTileMap);
                                qg.DrawImage(tileMap, 0, 0);

                                var quant = new WuQuantizer();
                                using var q = quant.QuantizeImage(newTileMap);

                                g.DrawImage(q, (x - minX) * tileSize, (height - (y - minY)) * tileSize);
                            }
                            else g.DrawImage(tileMap, (x - minX) * tileSize, (height - (y - minY)) * tileSize);
                        }
                    }
                }
            }
            bigMap.Save(Path.Combine(outputPath, "map.png"), ImageFormat.Png);
        }

        public int? ExtentX { get; set; } = null;
        public int? ExtentY { get; set; } = null;
        public int? MinX { get; set; } = null;
        public int? MaxY { get; set; } = null;
        public int? Depth { get; set; } = null;
        public void GenerateTiles(string inputPath, string outputPath, bool quantise = false, int resolution = 256)
        {
            // Get the current generated Morrowind tiles and work out the boundaries of the map.
            var files = Directory.GetFiles(inputPath);
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            foreach (var file in files)
            {
                var fn = file.Split("(")[1].Split(")")[0].Split(",");
                var x = int.Parse(fn[0]);
                var y = int.Parse(fn[1]);
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            // Calculate how many layers deep the final map will be.
            int cw = (maxX - minX) * resolution,
                ch = (maxY - minY) * resolution,
                depth = 0;
            while (cw > resolution || ch > resolution)
            {
                cw = cw / 2;
                ch = ch / 2;
                depth++;
            }

            MinX = minX;
            MaxY = maxY;

            // Copy (and convert) the initial tiles into the deepest layer.
            var tileMatches = files.ToDictionary(x => x, x =>
            {
                var m = _coordRegex.Match(x);
                return new[] { int.Parse(m.Groups["x"].Value), int.Parse(m.Groups["y"].Value) };
            });
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            var initialLayerPath = Path.Combine(outputPath, depth.ToString());
            if (!Directory.Exists(initialLayerPath)) Directory.CreateDirectory(initialLayerPath);
            for (var currX = minX; currX <= maxX; currX++)
            {
                var adjX = (currX - minX);
                var columnPath = Path.Combine(initialLayerPath, adjX.ToString());
                if (!Directory.Exists(columnPath)) Directory.CreateDirectory(columnPath);
                for (var currY = maxY; currY >= minY; currY--)
                {
                    var adjY = Math.Abs(currY - maxY);
                    if (tileMatches.Any(x => x.Value[0] == currX && x.Value[1] == currY))
                    {
                        var tilePath = tileMatches.FirstOrDefault(x => x.Value[0] == currX && x.Value[1] == currY).Key;
                        var newPath = Path.Combine(columnPath, $"{adjY}.png");
                        if (!File.Exists(newPath))
                        {
                            var bmp = (Bitmap)Image.FromFile(tilePath);
                            if (quantise)
                            {
                                var newBmp = new Bitmap(resolution, resolution, PixelFormat.Format32bppArgb);
                                using (Graphics g = Graphics.FromImage(newBmp))
                                {
                                    g.DrawImage(bmp, 0, 0);
                                }
                                var quant = new WuQuantizer();
                                using var q = quant.QuantizeImage(newBmp);
                                q.Save(newPath, ImageFormat.Png);
                            }
                            else bmp.Save(newPath, ImageFormat.Png);
                        }
                    }
                }
            }

            var lastLayerCols = (maxX - minX);
            var lastLayerRows = (maxY - minY);
            // Use each previous layer to generate the next one.
            for (int currLayer = (depth - 1); currLayer >= 0; currLayer--)
            {
                // Find the path of the last layer and how many columns and rows it has.
                var lastLayerPath = Path.Combine(outputPath, (currLayer + 1).ToString());
                
                var layerPath = Path.Combine(outputPath, currLayer.ToString());
                if (!Directory.Exists(layerPath)) Directory.CreateDirectory(layerPath);

                for (var currCol = 0; currCol <= lastLayerCols; currCol += 2)
                {
                    var colNum = (currCol / 2);
                    var colPath = Path.Combine(layerPath, colNum.ToString());
                    if (!Directory.Exists(colPath)) Directory.CreateDirectory(colPath);

                    for (var currRow = 0; currRow <= lastLayerRows; currRow += 2)
                    {
                        var rowNum = (currRow / 2);
                        var newPath = Path.Combine(colPath, $"{rowNum}.png");
                        if (!File.Exists(newPath))
                        {
                            var fileCount = 0;

                            var x0y0 = new Bitmap(resolution, resolution);
                            x0y0.MakeTransparent();
                            var x0y0Path = Path.Combine(lastLayerPath, currCol.ToString(), $"{currRow}.png");
                            if (File.Exists(x0y0Path))
                            {
                                x0y0 = (Bitmap)Bitmap.FromFile(x0y0Path);
                                fileCount++;
                            }

                            var x0y1 = new Bitmap(resolution, resolution);
                            x0y1.MakeTransparent();
                            var x0y1Path = Path.Combine(lastLayerPath, currCol.ToString(), $"{currRow + 1}.png");
                            if (File.Exists(x0y1Path))
                            {
                                x0y1 = (Bitmap)Bitmap.FromFile(x0y1Path);
                                fileCount++;
                            }

                            var x1y0 = new Bitmap(resolution, resolution);
                            x1y0.MakeTransparent();
                            var x1y0Path = Path.Combine(lastLayerPath, (currCol + 1).ToString(), $"{currRow}.png");
                            if (File.Exists(x1y0Path))
                            {
                                x1y0 = (Bitmap)Bitmap.FromFile(x1y0Path);
                                fileCount++;
                            }

                            var x1y1 = new Bitmap(resolution, resolution);
                            x1y1.MakeTransparent();
                            var x1y1Path = Path.Combine(lastLayerPath, (currCol + 1).ToString(), $"{currRow + 1}.png");
                            if (File.Exists(x1y1Path))
                            {
                                x1y1 = (Bitmap)Bitmap.FromFile(x1y1Path);
                                fileCount++;
                            }

                            // If there is at least one source image, draw a new image from it.
                            if (fileCount > 0)
                            {
                                var bmp = new Bitmap(resolution * 2, resolution * 2);
                                using (Graphics g = Graphics.FromImage(bmp))
                                {
                                    g.DrawImage(x0y0, 0, 0);
                                    g.DrawImage(x0y1, 0, resolution);
                                    g.DrawImage(x1y0, resolution, 0);
                                    g.DrawImage(x1y1, resolution, resolution);
                                }
                                var downscaled = ResizeImage(bmp, resolution, resolution);

                                if (quantise)
                                {
                                    var quant = new WuQuantizer();
                                    using var q = quant.QuantizeImage(downscaled);
                                    q.Save(newPath, ImageFormat.Png);
                                }
                                else downscaled.Save(newPath, ImageFormat.Png);
                            }
                        }
                    }
                }

                lastLayerCols = (int)Math.Ceiling(lastLayerCols / 2.0f);
                lastLayerRows = (int)Math.Ceiling(lastLayerRows / 2.0f);
            }

            // Export a script to use the map with leaflet.
            ExtentY = (maxY - minY) * resolution;
            ExtentX = (maxX - minX) * resolution;
            Depth = depth;
        }

        public string CellJson { get; set; } = null;
        public string DoorJson { get; set; } = null;
        public void GenerateCoordinates(string mwPath, string outputPath)
        {
            var iniPath = Path.Combine(mwPath, "Morrowind.ini");
            var dataPath = Path.Combine(mwPath, "Data Files");
            if (File.Exists(iniPath))
            {
                var jsonDir = Path.Combine(outputPath, "json");
                if (!Directory.Exists(jsonDir)) Directory.CreateDirectory("json");
                var iniLines = File.ReadAllLines(iniPath);
                var espLines = iniLines.Where(x => x.StartsWith("GameFile"));
                var espMods = espLines.Select(x => x.Split("=")[1].Split(".")[0]).ToArray();

                _espExtractor.ExtractEsps(dataPath, jsonDir, espMods, false);

                var cellDict = new Dictionary<string, TES3_Cell>();
                var cellDoors = new Dictionary<string, Dictionary<int, TES3_Cell_Reference>>();
                foreach (var key in espMods)
                {
                    var jsonPath = Path.Combine(jsonDir, $"{key}.json");
                    if (File.Exists(jsonPath)) { 
                    // Deserialize the export into a dynamic.
                    dynamic jsonData = JsonConvert.DeserializeObject(File.ReadAllText(jsonPath));
                    // Iterate over all the records.
                    foreach (var record in (JArray)jsonData)
                    {
                        // Convert the game record to a usable class.
                        var objectType = record.Value<string>("type");
                            switch (objectType)
                            {
                                case "Cell":
                                    var cell = JsonConvert.DeserializeObject<TES3_Cell>(JsonConvert.SerializeObject(record));
                                    if (!string.IsNullOrWhiteSpace(cell.region) && !cell.data.flags.Contains("IS_INTERIOR"))
                                    {
                                        if (cell.data != null && cell.data.grid != null && cell.data.grid.Length == 2)
                                        {
                                            var cellId = $"{cell.data.grid[0]}_{cell.data.grid[1]}";
                                            cellDict[cellId] = cell;
                                            if (!cellDoors.ContainsKey(cellId))
                                                cellDoors[cellId] = [];
                                            foreach (var doorRef in cell.references.Where(r => r.destination != null && !string.IsNullOrWhiteSpace(r.destination.cell)))
                                            {
                                                cellDoors[cellId][doorRef.refr_index] = doorRef;
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                if (cellDict.Any())
                {
                    var cellOffsetX = MinX ?? cellDict.Select(x => x.Value.data.grid[0]).Min();
                    var cellOffsetY = MaxY ?? cellDict.Select(x => x.Value.data.grid[1]).Max();

                    var doorDict = cellDoors.SelectMany(x => x.Value.Values).Select(x => new { Name = x.destination.cell, Grid = new[] { (x.translation[0] / 8192) - cellOffsetX, (x.translation[1] / 8192) - cellOffsetY } }).ToList();

                    var namedCells = cellDict.Where(x => !string.IsNullOrWhiteSpace(x.Value.name))
                        .Select(x => new { Name = x.Value.name, Grid = new[] { x.Value.data.grid[0] - cellOffsetX, x.Value.data.grid[1] - cellOffsetY } }).ToList();
                    CellJson = JsonConvert.SerializeObject(namedCells);
                    DoorJson = JsonConvert.SerializeObject(doorDict);
                }

                Directory.Delete(jsonDir, true);
            }
        }

        public void GenerateDemo(string outputPath)
        {
            var htmlTemplate = @"
<!DOCTYPE html>
<html>
    <head>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
        <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css' />
        <script src='https://code.jquery.com/jquery-3.7.1.min.js'></script>
        <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
        <style>
            .leaflet-tooltip { background-color: black; border: 1px solid #caa560; border-radius: 0; color: #caa560; }
            .leaflet-tooltip::before { display: none; }
        </style>
    </head>
    <body style='margin:0;padding:0;height:100vh;width:100vw;'>
        <div id='map' style='height:100%;width:100%;background-color:#1e1c18;'></div>
        <script>
            {scriptTemplate}
        </script>
        <script>{cellScript}</script>
        <script>{doorScript}</script>
    </body>
</html>
";

            var scriptTemplate = @"
$(document).ready(() => createMap('map', ''));

// Stores whether the map markers are toggled on.
var markerToggleOn = true;

// Stores the leaflet map object.
var map = null;

// Function to create a map, requires the id of a div to create the map in and the path to the tiles (an empty string if the tile folders are located next to this file).
function createMap(divId, tilePath) {

    // Create the map using the tile images in folders.
    var mapExtent = [0, -{extentY}, {extentX}, 0];
    var mapMinZoom = 0;
    var mapMaxZoom = {depth};
    var mapMinResolution = Math.pow(2, mapMaxZoom);
    var crs = L.CRS.Simple;
    crs.transformation = new L.Transformation(1, -mapExtent[0], -1, mapExtent[3]);
    crs.scale=function(zoom){return Math.pow(2, zoom)/mapMinResolution;};
    crs.zoom=function(scale){return Math.log(scale*mapMinResolution)/Math.LN2;};
    map = new L.Map(divId,{maxZoom: mapMaxZoom*2,minZoom: mapMinZoom,crs: crs,attributionControl: false});
    var layer = L.tileLayer(tilePath + '{z}/{x}/{y}.png',{minZoom:mapMinZoom,maxNativeZoom:mapMaxZoom,maxZoom:mapMaxZoom*2,noWrap:true,tms:false}).addTo(map);
    map.fitBounds([crs.unproject(L.point(mapExtent[2], mapExtent[3])),crs.unproject(L.point(mapExtent[0], mapExtent[1]))]);
          
    // Prepare layers and the icon to use for map markers.
    var mwIcon = L.icon({
        iconUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAMCAYAAABWdVznAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAO0lEQVQokWP8//8/AymAhYGBgWHb/ByidHklTmFkIsl4mA1IgBGHOrgLSLZhOGhADyWC8UGyDYykJg0AvqIMFsgeioQAAAAASUVORK5CYII=',
        shadowUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAMCAYAAABWdVznAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAGElEQVQokWNkYGD4z0ACYCJF8aiGkaQBAGOyARfDSz7hAAAAAElFTkSuQmCC',
        iconSize: [12,12],
        shadowSize: [12,12]
    });
    var doorMarkers = new L.FeatureGroup();
    var cellMarkers = new L.FeatureGroup();

    try {

        // If cell data is present, add cell markers to the layer.
        if (mapCells != null) {
            for (var ix in mapCells) {
                L.marker([((mapCells[ix].Grid[1])*256)-128, ((mapCells[ix].Grid[0])*256)+128], {icon: mwIcon}).addTo(cellMarkers).bindTooltip(mapCells[ix].Name);
            }
        }

        // If door data is present, add door markers to the layer.
        if (mapDoors != null) {
            for (var ix in mapDoors) {
                L.marker([((mapDoors[ix].Grid[1])*256)-256, ((mapDoors[ix].Grid[0])*256)], {icon: mwIcon}).addTo(doorMarkers).bindTooltip(mapDoors[ix].Name);
            }
        }

        // Add appropriate markers to the map for the current zoom level.
        if (map.getZoom() >= mapMaxZoom) { doorMarkers.addTo(map); }
        else { cellMarkers.addTo(map); }

        // Trigger switch between markers when zoom level changes.
        map.on('zoomend', function () {
            if (markerToggleOn) {
                if (map.getZoom() >= mapMaxZoom) { map.addLayer(doorMarkers); map.removeLayer(cellMarkers); }
                else { map.addLayer(cellMarkers); map.removeLayer(doorMarkers); }
            }
        });

        // Add a toggle option to show or hide the map markers.
        L.Control.MarkerToggle = L.Control.extend({
            options: {position: 'topleft'},
            onAdd: function (map) {
                var container = L.DomUtil.create('div', 'leaflet-bar leaflet-control leaflet-control-custom');
                container.style.fontSize = '1rem';
                container.innerHTML = '<i class=""fa-solid fa-location-dot""></i>';
                container.style.backgroundColor = 'white';
                container.style.width = '30px';
                container.style.height = '30px';
                container.style.lineHeight = '30px';
                container.style.textAlign = 'center';
                container.onclick = function () {
                    if (markerToggleOn == true) { map.removeLayer(cellMarkers); map.removeLayer(doorMarkers); markerToggleOn = false; }
                    else if (map.getZoom() >= mapMaxZoom) { map.addLayer(doorMarkers); markerToggleOn = true; }
                    else { map.addLayer(cellMarkers); markerToggleOn = true; }
                }
                return container;
            }
        });
        var markerToggle = new L.Control.MarkerToggle({position:'topleft'}).addTo(map);
    }
    catch {}
}
";

            scriptTemplate = scriptTemplate
                .Replace("{extentX}", ExtentX.ToString())
                .Replace("{extentY}", ExtentY.ToString())
                .Replace("{depth}", Depth.ToString());

            htmlTemplate = htmlTemplate
                .Replace("{scriptTemplate}", scriptTemplate)
                .Replace("{cellScript}", $"var mapCells = {CellJson ?? "null"};")
                .Replace("{doorScript}", $"var mapDoors = {DoorJson ?? "null"};");

            var htmlPath = Path.Combine(outputPath, "map.html");
            File.WriteAllText(htmlPath, htmlTemplate);
        }
    }
}