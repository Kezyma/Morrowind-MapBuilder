using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MorrowindMapGen.Core.MapGeneration;

/// <summary>
/// Processes map tiles and generates zoom pyramid for web display.
/// </summary>
public partial class TileProcessor
{
    private readonly ILogger<TileProcessor> _logger;
    private const int TileSize = 256;

    /// <summary>
    /// Maximum degree of parallelism for tile processing operations.
    /// Defaults to processor count.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    public TileProcessor(ILogger<TileProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes tiles from a source directory and generates a zoom pyramid.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing source tiles.</param>
    /// <param name="outputDirectory">Directory to output the zoom pyramid.</param>
    /// <param name="layers">Optional additional tile layers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Metadata about the processed map.</returns>
    public async Task<MapMetadata> ProcessTilesAsync(
        string sourceDirectory,
        string outputDirectory,
        List<LayerInfo>? layers = null,
        CancellationToken cancellationToken = default)
    {
        layers ??= new List<LayerInfo>();
        var hasLayers = layers.Count > 0;

        _logger.LogInformation("Processing tiles from {Source}...", sourceDirectory);

        // Find and parse all tile files from base layer
        var baseTiles = ParseTileFiles(sourceDirectory);

        if (baseTiles.Count == 0)
        {
            throw new InvalidOperationException($"No tile files found in {sourceDirectory}");
        }

        _logger.LogInformation("Found {Count} base tiles", baseTiles.Count);

        // Calculate GLOBAL bounds from base map AND all layers for alignment
        int globalMinX = baseTiles.Min(t => t.X);
        int globalMaxX = baseTiles.Max(t => t.X);
        int globalMinY = baseTiles.Min(t => t.Y);
        int globalMaxY = baseTiles.Max(t => t.Y);

        // Extend bounds to include all layers
        foreach (var layer in layers)
        {
            if (!Directory.Exists(layer.InputPath))
            {
                _logger.LogWarning("Layer path does not exist: {Path}", layer.InputPath);
                continue;
            }

            var layerTiles = ParseTileFiles(layer.InputPath);
            if (layerTiles.Count > 0)
            {
                globalMinX = Math.Min(globalMinX, layerTiles.Min(t => t.X));
                globalMaxX = Math.Max(globalMaxX, layerTiles.Max(t => t.X));
                globalMinY = Math.Min(globalMinY, layerTiles.Min(t => t.Y));
                globalMaxY = Math.Max(globalMaxY, layerTiles.Max(t => t.Y));
                _logger.LogInformation("Layer '{Name}' has {Count} tiles", layer.Name, layerTiles.Count);
            }
        }

        var widthInTiles = globalMaxX - globalMinX + 1;
        var heightInTiles = globalMaxY - globalMinY + 1;

        _logger.LogInformation("Global map bounds: X=[{MinX}, {MaxX}], Y=[{MinY}, {MaxY}] ({Width}x{Height} tiles)",
            globalMinX, globalMaxX, globalMinY, globalMaxY, widthInTiles, heightInTiles);

        // Calculate zoom depth based on global bounds
        var maxZoom = CalculateZoomDepth(widthInTiles * TileSize, heightInTiles * TileSize);
        _logger.LogInformation("Zoom levels: 0 to {MaxZoom}", maxZoom);

        // Create output directory
        Directory.CreateDirectory(outputDirectory);

        // Process base layer
        var baseOutputDir = hasLayers ? Path.Combine(outputDirectory, "base") : outputDirectory;
        _logger.LogInformation("Processing base layer...");

        // Determine fallback path for base layer
        var baseFallbackSrc = Path.Combine(sourceDirectory, "fallback.png");
        string? baseFallbackPath = File.Exists(baseFallbackSrc) ? baseFallbackSrc : null;

        await ProcessSingleLayerAsync(baseTiles, baseOutputDir, maxZoom, globalMinX, globalMaxY, baseFallbackPath, cancellationToken);

        // Copy fallback.png for base layer if present
        if (baseFallbackPath != null)
        {
            File.Copy(baseFallbackSrc, Path.Combine(baseOutputDir, "fallback.png"), true);
        }

        // Process additional layers with SAME global bounds for alignment (in parallel)
        // Pre-filter and prepare layer data
        var validLayers = new List<(LayerInfo Layer, List<TileInfo> Tiles, string OutputDir, string? FallbackPath)>();
        foreach (var layer in layers)
        {
            if (!Directory.Exists(layer.InputPath)) continue;

            var layerTiles = ParseTileFiles(layer.InputPath);
            if (layerTiles.Count == 0)
            {
                _logger.LogWarning("No tiles found in layer '{Name}'", layer.Name);
                continue;
            }

            var layerOutputDir = Path.Combine(outputDirectory, layer.Name);
            var fallbackSrc = Path.Combine(layer.InputPath, "fallback.png");
            string? layerFallbackPath = File.Exists(fallbackSrc) ? fallbackSrc : null;

            validLayers.Add((layer, layerTiles, layerOutputDir, layerFallbackPath));
        }

        if (validLayers.Count > 0)
        {
            _logger.LogInformation("Processing {Count} additional layers in parallel...", validLayers.Count);

            var layerOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(validLayers, layerOptions, async (layerData, ct) =>
            {
                var (layer, layerTiles, layerOutputDir, layerFallbackPath) = layerData;
                _logger.LogInformation("Processing layer '{Name}' ({Count} tiles)...", layer.Name, layerTiles.Count);

                await ProcessSingleLayerAsync(layerTiles, layerOutputDir, maxZoom, globalMinX, globalMaxY, layerFallbackPath, ct);

                // Copy fallback.png if present
                if (layerFallbackPath != null)
                {
                    var fallbackSrc = Path.Combine(layer.InputPath, "fallback.png");
                    File.Copy(fallbackSrc, Path.Combine(layerOutputDir, "fallback.png"), true);
                }
            });
        }

        var metadata = new MapMetadata
        {
            MinX = globalMinX,
            MaxX = globalMaxX,
            MinY = globalMinY,
            MaxY = globalMaxY,
            MaxZoom = maxZoom,
            TileSize = TileSize,
            TotalTiles = baseTiles.Count
        };

        _logger.LogInformation("Tile processing complete. Generated zoom pyramid with {Levels} levels.",
            maxZoom + 1);

        return metadata;
    }

    /// <summary>
    /// Processes a single layer's tiles into a zoom pyramid.
    /// </summary>
    private async Task ProcessSingleLayerAsync(
        List<TileInfo> tiles,
        string outputDirectory,
        int maxZoom,
        int globalMinX,
        int globalMaxY,
        string? fallbackPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);

        // Copy original tiles to the deepest zoom level using global bounds
        await CopyTilesToZoomLevelAsync(tiles, outputDirectory, maxZoom, globalMinX, globalMaxY, cancellationToken);

        // Generate parent zoom levels
        for (int zoom = maxZoom - 1; zoom >= 0; zoom--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GenerateZoomLevelAsync(outputDirectory, zoom, fallbackPath, cancellationToken);
        }
    }

    /// <summary>
    /// Parses tile files from a directory, extracting coordinates from filenames.
    /// </summary>
    private List<TileInfo> ParseTileFiles(string directory)
    {
        var tiles = new List<TileInfo>();
        var files = Directory.GetFiles(directory, "*.png", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directory, "*.bmp", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            // Try different coordinate patterns
            var match = CoordinatePattern1().Match(fileName);
            if (!match.Success)
            {
                match = CoordinatePattern2().Match(fileName);
            }

            if (match.Success)
            {
                tiles.Add(new TileInfo
                {
                    X = int.Parse(match.Groups["x"].Value),
                    Y = int.Parse(match.Groups["y"].Value),
                    FilePath = file
                });
            }
            else
            {
                _logger.LogWarning("Could not parse coordinates from tile filename: {File}", fileName);
            }
        }

        return tiles;
    }

    /// <summary>
    /// Calculates the number of zoom levels needed.
    /// </summary>
    private static int CalculateZoomDepth(int width, int height)
    {
        int depth = 0;
        while (width > TileSize || height > TileSize)
        {
            depth++;
            width = (width + 1) / 2;
            height = (height + 1) / 2;
        }
        return depth;
    }

    /// <summary>
    /// Copies source tiles to the deepest zoom level with coordinate normalization.
    /// </summary>
    private async Task CopyTilesToZoomLevelAsync(
        List<TileInfo> tiles,
        string outputDirectory,
        int zoomLevel,
        int minX,
        int maxY,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Copying {Count} tiles to zoom level {Zoom} (parallelism: {Parallelism})...",
            tiles.Count, zoomLevel, MaxDegreeOfParallelism);

        var zoomDir = Path.Combine(outputDirectory, zoomLevel.ToString());

        // Pre-create all X directories to avoid race conditions
        var uniqueXValues = tiles.Select(t => t.X - minX).Distinct();
        foreach (var x in uniqueXValues)
        {
            Directory.CreateDirectory(Path.Combine(zoomDir, x.ToString()));
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(tiles, parallelOptions, async (tile, ct) =>
        {
            // Normalize X coordinate to 0-based (left to right)
            var outputX = tile.X - minX;

            // Invert Y axis: Morrowind Y increases northward, but web tiles expect Y to increase downward
            var outputY = maxY - tile.Y;

            var outputPath = Path.Combine(zoomDir, outputX.ToString(), $"{outputY}.png");

            if (!File.Exists(outputPath))
            {
                // Convert to PNG and resize if necessary
                using var image = await Image.LoadAsync<Rgba32>(tile.FilePath, ct);

                if (image.Width != TileSize || image.Height != TileSize)
                {
                    image.Mutate(x => x.Resize(TileSize, TileSize));
                }

                await image.SaveAsPngAsync(outputPath, ct);
            }
        });

        _logger.LogDebug("Copied {Count} tiles to zoom level {Zoom}", tiles.Count, zoomLevel);
    }

    /// <summary>
    /// Generates a parent zoom level by combining tiles from the child level.
    /// </summary>
    private async Task GenerateZoomLevelAsync(
        string outputDirectory,
        int zoomLevel,
        string? fallbackPath,
        CancellationToken cancellationToken)
    {
        var childZoomLevel = zoomLevel + 1;
        var childZoomDir = Path.Combine(outputDirectory, childZoomLevel.ToString());
        var parentZoomDir = Path.Combine(outputDirectory, zoomLevel.ToString());

        if (!Directory.Exists(childZoomDir))
        {
            _logger.LogWarning("Child zoom directory not found: {Dir}", childZoomDir);
            return;
        }

        // Get all child tiles grouped by parent tile
        var childXDirs = Directory.GetDirectories(childZoomDir)
            .Select(d => int.Parse(Path.GetFileName(d)))
            .ToList();

        if (childXDirs.Count == 0) return;

        var minChildX = childXDirs.Min();
        var maxChildX = childXDirs.Max();

        // Find Y range
        var allChildYs = new List<int>();
        foreach (var xDir in Directory.GetDirectories(childZoomDir))
        {
            var yFiles = Directory.GetFiles(xDir, "*.png")
                .Select(f => int.Parse(Path.GetFileNameWithoutExtension(f)));
            allChildYs.AddRange(yFiles);
        }

        if (allChildYs.Count == 0) return;

        var minChildY = allChildYs.Min();
        var maxChildY = allChildYs.Max();

        // Build list of parent tile coordinates to process
        var parentTileCoords = new List<(int X, int Y)>();
        for (int parentX = minChildX / 2; parentX <= maxChildX / 2; parentX++)
        {
            for (int parentY = minChildY / 2; parentY <= maxChildY / 2; parentY++)
            {
                parentTileCoords.Add((parentX, parentY));
            }
        }

        _logger.LogInformation("Generating zoom level {Zoom} ({Count} potential tiles, parallelism: {Parallelism})...",
            zoomLevel, parentTileCoords.Count, MaxDegreeOfParallelism);

        // Pre-create all parent X directories to avoid race conditions
        var uniqueParentXValues = parentTileCoords.Select(c => c.X).Distinct();
        foreach (var x in uniqueParentXValues)
        {
            Directory.CreateDirectory(Path.Combine(parentZoomDir, x.ToString()));
        }

        // Load fallback tile data once if it exists (will clone for each thread)
        byte[]? fallbackTileData = null;
        if (fallbackPath != null && File.Exists(fallbackPath))
        {
            using var fallbackImage = await Image.LoadAsync<Rgba32>(fallbackPath, cancellationToken);
            fallbackImage.Mutate(x => x.Resize(TileSize / 2, TileSize / 2));
            using var ms = new MemoryStream();
            await fallbackImage.SaveAsPngAsync(ms, cancellationToken);
            fallbackTileData = ms.ToArray();
        }

        var tilesGenerated = 0;
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(parentTileCoords, parallelOptions, async (coord, ct) =>
        {
            var (parentX, parentY) = coord;
            var childTilesFound = 0;
            var parentPath = Path.Combine(parentZoomDir, parentX.ToString(), $"{parentY}.png");
            if (!File.Exists(parentPath))
            {
                using var parentTile = new Image<Rgba32>(TileSize, TileSize);

                // Load fallback tile for this thread if available
                Image<Rgba32>? fallbackTile = null;
                if (fallbackTileData != null)
                {
                    fallbackTile = Image.Load<Rgba32>(fallbackTileData);
                }

                try
                {
                    // Combine 4 child tiles
                    for (int dx = 0; dx <= 1; dx++)
                    {
                        for (int dy = 0; dy <= 1; dy++)
                        {
                            var childX = parentX * 2 + dx;
                            var childY = parentY * 2 + dy;
                            var childPath = Path.Combine(childZoomDir, childX.ToString(), $"{childY}.png");

                            // Calculate position in parent tile
                            var destX = dx * (TileSize / 2);
                            var destY = dy * (TileSize / 2);

                            if (File.Exists(childPath))
                            {
                                using var childTile = await Image.LoadAsync<Rgba32>(childPath, ct);

                                // Resize child tile to half size
                                childTile.Mutate(x => x.Resize(TileSize / 2, TileSize / 2));

                                // Draw child tile onto parent
                                parentTile.Mutate(x => x.DrawImage(childTile, new Point(destX, destY), 1f));
                                childTilesFound++;
                            }
                            else if (fallbackTile != null)
                            {
                                // Use fallback for missing quadrant
                                parentTile.Mutate(x => x.DrawImage(fallbackTile, new Point(destX, destY), 1f));
                            }
                        }
                    }

                    // Only save if at least 1 child tile existed
                    if (childTilesFound > 0)
                    {
                        await parentTile.SaveAsPngAsync(parentPath, ct);
                        Interlocked.Increment(ref tilesGenerated);
                    }
                }
                finally
                {
                    fallbackTile?.Dispose();
                }
            }
        });

        _logger.LogDebug("Generated {Count} tiles for zoom level {Zoom}", tilesGenerated, zoomLevel);
    }

    // Pattern: "(x,y)" - Morrowind CreateMaps format
    [GeneratedRegex(@"\((?<x>-?\d+),\s*(?<y>-?\d+)\)")]
    private static partial Regex CoordinatePattern1();

    // Pattern: "x_y" or "x-y" - alternative formats
    [GeneratedRegex(@"^(?<x>-?\d+)[_-](?<y>-?\d+)$")]
    private static partial Regex CoordinatePattern2();
}
