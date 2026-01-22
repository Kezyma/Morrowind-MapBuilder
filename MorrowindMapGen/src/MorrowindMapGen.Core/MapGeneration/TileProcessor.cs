using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MorrowindMapGen.Core.MapGeneration;

/// <summary>
/// Processes map tiles and generates zoom pyramid for web display.
/// </summary>
public partial class TileProcessor
{
    private readonly ILogger<TileProcessor> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private const int BaseTileSize = 256;

    /// <summary>
    /// Maximum degree of parallelism for tile processing operations.
    /// Defaults to processor count.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Processing options (format, tile size).
    /// </summary>
    public TileProcessingOptions Options { get; set; } = new();

    public TileProcessor(ILogger<TileProcessor> logger, ILoggerFactory loggerFactory, TileProcessingOptions? options = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        Options = options ?? new TileProcessingOptions();
    }

    /// <summary>
    /// Analyzes tiles in a directory and returns metadata without generating zoom pyramid.
    /// Useful for marker extraction when web map generation is skipped.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing source tiles.</param>
    /// <returns>Metadata about the tiles.</returns>
    public MapMetadata AnalyzeTiles(string sourceDirectory)
    {
        _logger.LogInformation("Analyzing tiles from {Source}...", sourceDirectory);

        var tiles = ParseTileFiles(sourceDirectory);

        if (tiles.Count == 0)
        {
            throw new InvalidOperationException($"No tile files found in {sourceDirectory}");
        }

        var minX = tiles.Min(t => t.X);
        var maxX = tiles.Max(t => t.X);
        var minY = tiles.Min(t => t.Y);
        var maxY = tiles.Max(t => t.Y);

        var widthInTiles = maxX - minX + 1;
        var heightInTiles = maxY - minY + 1;

        var maxZoom = CalculateZoomDepth(widthInTiles * BaseTileSize, heightInTiles * BaseTileSize);

        if (Options.Use512pxTiles)
        {
            maxZoom = Math.Max(0, maxZoom - 1);
        }

        return new MapMetadata
        {
            MinX = minX,
            MaxX = maxX,
            MinY = minY,
            MaxY = maxY,
            MaxZoom = maxZoom,
            TileSize = Options.TileSize,
            TotalTiles = tiles.Count
        };
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
        var tileSize = Options.TileSize;
        var fileExt = Options.FileExtension;

        _logger.LogInformation("Processing tiles from {Source}...", sourceDirectory);
        _logger.LogInformation("Output format: {Format}, Tile size: {Size}px",
            Options.OutputFormat, tileSize);

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
            if (string.IsNullOrEmpty(layer.InputPath) || !Directory.Exists(layer.InputPath))
            {
                if (!string.IsNullOrEmpty(layer.InputPath))
                {
                    _logger.LogWarning("Layer path does not exist: {Path}", layer.InputPath);
                }
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

        // Calculate zoom depth based on global bounds using BASE tile size (256)
        // Then adjust for 512px mode
        var maxZoom = CalculateZoomDepth(widthInTiles * BaseTileSize, heightInTiles * BaseTileSize);

        // In 512px mode, we have one less zoom level since tiles are 2x larger
        if (Options.Use512pxTiles)
        {
            maxZoom = Math.Max(0, maxZoom - 1);
            _logger.LogInformation("512px mode: adjusted max zoom to {MaxZoom}", maxZoom);
        }

        _logger.LogInformation("Zoom levels: 0 to {MaxZoom}", maxZoom);

        // Create output directory and tiles subdirectory
        Directory.CreateDirectory(outputDirectory);
        var tilesDir = Path.Combine(outputDirectory, "tiles");
        Directory.CreateDirectory(tilesDir);

        // Process base layer - always use tiles/base/
        var baseOutputDir = Path.Combine(tilesDir, "base");
        _logger.LogInformation("Processing base layer...");

        // Determine fallback path for base layer
        string? baseFallbackPath = FindFallbackImage(sourceDirectory);

        await ProcessSingleLayerAsync(baseTiles, baseOutputDir, maxZoom, globalMinX, globalMaxY, baseFallbackPath, cancellationToken);

        // Copy fallback image for base layer if present (convert to output format)
        if (baseFallbackPath != null)
        {
            await ConvertFallbackImageAsync(baseFallbackPath, Path.Combine(baseOutputDir, "fallback" + fileExt), cancellationToken);
        }

        // Process additional layers with SAME global bounds for alignment (in parallel)
        // Pre-filter and prepare layer data, analyzing source tile sizes
        var tileAnalyzer = new LayerTileAnalyzer(_loggerFactory.CreateLogger<LayerTileAnalyzer>());
        var validLayers = new List<(LayerInfo Layer, List<TileInfo> Tiles, string OutputDir, string? FallbackPath, LayerScaleInfo? ScaleInfo)>();

        foreach (var layer in layers)
        {
            if (string.IsNullOrEmpty(layer.InputPath) || !Directory.Exists(layer.InputPath)) continue;

            var layerTiles = ParseTileFiles(layer.InputPath);
            if (layerTiles.Count == 0)
            {
                _logger.LogWarning("No tiles found in layer '{Name}'", layer.Name);
                continue;
            }

            var layerOutputDir = Path.Combine(tilesDir, layer.Name);
            string? layerFallbackPath = FindFallbackImage(layer.InputPath);
            layer.HasFallback = layerFallbackPath != null;

            // Analyze source tile size and calculate scaling
            var scaleInfo = tileAnalyzer.AnalyzeSourceTiles(layer.InputPath);
            if (scaleInfo != null)
            {
                layer.SourceTileSize = scaleInfo.SourceTileSize;
                layer.ScaledTileSize = scaleInfo.ScaledTileSize;

                // Calculate max native zoom for this layer
                // If source tiles are smaller than 256, we have fewer zoom levels
                var layerMaxZoom = maxZoom - scaleInfo.ZoomLevelDifference;
                layer.MaxNativeZoom = Math.Max(0, layerMaxZoom);

                _logger.LogInformation(
                    "Layer '{Name}': source={SourceSize}px, scaled={ScaledSize}px, tilesPerOutput={TilesPerOutput}x{TilesPerOutput2}, maxNativeZoom={MaxZoom}",
                    layer.Name, scaleInfo.SourceTileSize, scaleInfo.ScaledTileSize,
                    scaleInfo.TilesPerOutputTile, scaleInfo.TilesPerOutputTile, layer.MaxNativeZoom);
            }
            else
            {
                // Default to standard 256px processing
                layer.SourceTileSize = BaseTileSize;
                layer.ScaledTileSize = BaseTileSize;
                layer.MaxNativeZoom = maxZoom;
            }

            validLayers.Add((layer, layerTiles, layerOutputDir, layerFallbackPath, scaleInfo));
        }

        if (validLayers.Count > 0)
        {
            _logger.LogInformation("Processing {Count} additional layers in parallel...", validLayers.Count);

            var layerParallelOpts = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(validLayers, layerParallelOpts, async (layerData, ct) =>
            {
                var (layer, layerTiles, layerOutputDir, layerFallbackPath, scaleInfo) = layerData;
                _logger.LogInformation("Processing layer '{Name}' ({Count} tiles)...", layer.Name, layerTiles.Count);

                // Check if we need scaled processing (source tiles smaller than 256)
                if (scaleInfo != null && LayerTileAnalyzer.NeedsStitching(scaleInfo))
                {
                    await ProcessScaledLayerAsync(layerTiles, layerOutputDir, layer.MaxNativeZoom,
                        globalMinX, globalMaxY, scaleInfo, layerFallbackPath, ct);
                }
                else
                {
                    await ProcessSingleLayerAsync(layerTiles, layerOutputDir, layer.MaxNativeZoom,
                        globalMinX, globalMaxY, layerFallbackPath, ct);
                }

                // Copy fallback image if present (convert to output format)
                if (layerFallbackPath != null)
                {
                    await ConvertFallbackImageAsync(layerFallbackPath, Path.Combine(layerOutputDir, "fallback" + fileExt), ct);
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
            TileSize = tileSize,
            TotalTiles = baseTiles.Count,
            HasFallback = baseFallbackPath != null
        };

        _logger.LogInformation("Tile processing complete. Generated zoom pyramid with {Levels} levels.",
            maxZoom + 1);

        return metadata;
    }

    /// <summary>
    /// Converts a fallback image to the output format.
    /// </summary>
    private async Task ConvertFallbackImageAsync(string sourcePath, string outputPath, CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync<Rgba32>(sourcePath, cancellationToken);

        // Resize to current tile size if needed
        var tileSize = Options.TileSize;
        if (image.Width != tileSize || image.Height != tileSize)
        {
            image.Mutate(x => x.Resize(tileSize, tileSize));
        }

        await SaveImageAsync(image, outputPath, cancellationToken);
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
    /// Processes a layer with smaller source tiles by scaling and stitching them.
    /// Source tiles are scaled to their nearest power of 2, then stitched into 256px intermediate tiles.
    /// In 512px mode, those 256px tiles are further stitched into 512px output tiles.
    /// For example, 41px tiles are scaled to 64px, then 4x4 tiles are combined into 256px.
    /// </summary>
    private async Task ProcessScaledLayerAsync(
        List<TileInfo> tiles,
        string outputDirectory,
        int maxZoom,
        int globalMinX,
        int globalMaxY,
        LayerScaleInfo scaleInfo,
        string? fallbackPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);

        var scaledSize = scaleInfo.ScaledTileSize;
        var tilesPerOutput = scaleInfo.TilesPerOutputTile; // e.g., 4 for 64px -> 256px
        var fileExt = Options.FileExtension;
        var use512 = Options.Use512pxTiles;

        _logger.LogInformation(
            "Processing scaled layer: scaling {Source}px to {Scaled}px, stitching {Grid}x{Grid2} tiles per 256px{Mode}",
            scaleInfo.SourceTileSize, scaledSize, tilesPerOutput, tilesPerOutput,
            use512 ? ", then 2x2 into 512px" : "");

        // Load fallback tile data once if it exists (scaled to the target size)
        byte[]? fallbackTileData = null;
        if (fallbackPath != null && File.Exists(fallbackPath))
        {
            using var fallbackImage = await Image.LoadAsync<Rgba32>(fallbackPath, cancellationToken);
            _logger.LogInformation("Loaded fallback image ({OrigW}x{OrigH}), scaling to {Size}px for stitching",
                fallbackImage.Width, fallbackImage.Height, scaledSize);
            fallbackImage.Mutate(x => x.Resize(scaledSize, scaledSize));
            using var ms = new MemoryStream();
            await fallbackImage.SaveAsPngAsync(ms, cancellationToken);
            fallbackTileData = ms.ToArray();
        }
        else if (fallbackPath != null)
        {
            _logger.LogWarning("Fallback path specified but file not found: {Path}", fallbackPath);
        }
        else
        {
            _logger.LogDebug("No fallback image configured for this layer");
        }

        // Build lookup of tiles by their game coordinates
        var tilesByCoord = tiles.ToDictionary(t => (t.X, t.Y), t => t);

        // Calculate 256px intermediate tile coordinates
        // Each intermediate tile covers tilesPerOutput x tilesPerOutput source tiles
        // Normalized coords: (gameX - globalMinX), (globalMaxY - gameY)
        // Intermediate coords: normalized / tilesPerOutput

        var intermediateCoords = new HashSet<(int X, int Y)>();
        foreach (var tile in tiles)
        {
            var normX = tile.X - globalMinX;
            var normY = globalMaxY - tile.Y;
            var outX = normX / tilesPerOutput;
            var outY = normY / tilesPerOutput;
            intermediateCoords.Add((outX, outY));
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        if (use512)
        {
            // 512px mode: create 512px output tiles directly by combining source tiles
            // Each 512px tile covers 2x2 of 256px intermediate tiles
            var outputCoords = new HashSet<(int X, int Y)>();
            foreach (var coord in intermediateCoords)
            {
                outputCoords.Add((coord.X / 2, coord.Y / 2));
            }

            var zoomDir = Path.Combine(outputDirectory, maxZoom.ToString());

            // Pre-create output directories
            var uniqueXValues = outputCoords.Select(c => c.X).Distinct();
            foreach (var x in uniqueXValues)
            {
                Directory.CreateDirectory(Path.Combine(zoomDir, x.ToString()));
            }

            _logger.LogInformation("Creating {Count} stitched 512px tiles at zoom {Zoom}...",
                outputCoords.Count, maxZoom);

            await Parallel.ForEachAsync(outputCoords, parallelOptions, async (coord, ct) =>
            {
                var (outX, outY) = coord;
                var outputPath = Path.Combine(zoomDir, outX.ToString(), $"{outY}{fileExt}");

                if (!File.Exists(outputPath))
                {
                    // Load fallback tile for this thread if available
                    Image<Rgba32>? fallbackTile = null;
                    if (fallbackTileData != null)
                    {
                        fallbackTile = Image.Load<Rgba32>(fallbackTileData);
                    }

                    try
                    {
                        using var outputTile = new Image<Rgba32>(512, 512);
                        var tilesFound = 0;

                        // Each 512px output covers 2x2 intermediate (256px) tiles
                        for (int ix = 0; ix <= 1; ix++)
                        {
                            for (int iy = 0; iy <= 1; iy++)
                            {
                                var intermediateX = outX * 2 + ix;
                                var intermediateY = outY * 2 + iy;

                                // Create the 256px intermediate tile in memory
                                using var intermediateTile = new Image<Rgba32>(BaseTileSize, BaseTileSize);
                                var intermediateFound = 0;

                                // Load and place source tiles for this intermediate tile
                                for (int dx = 0; dx < tilesPerOutput; dx++)
                                {
                                    for (int dy = 0; dy < tilesPerOutput; dy++)
                                    {
                                        var normX = intermediateX * tilesPerOutput + dx;
                                        var normY = intermediateY * tilesPerOutput + dy;
                                        var srcX = normX + globalMinX;
                                        var srcY = globalMaxY - normY;

                                        if (tilesByCoord.TryGetValue((srcX, srcY), out var srcTile))
                                        {
                                            using var image = await Image.LoadAsync<Rgba32>(srcTile.FilePath, ct);

                                            if (image.Width != scaledSize || image.Height != scaledSize)
                                            {
                                                image.Mutate(x => x.Resize(scaledSize, scaledSize));
                                            }

                                            var destX = dx * scaledSize;
                                            var destY = dy * scaledSize;
                                            intermediateTile.Mutate(x => x.DrawImage(image, new Point(destX, destY), 1f));
                                            intermediateFound++;
                                        }
                                        else if (fallbackTile != null)
                                        {
                                            // Use fallback for missing tile
                                            var destX = dx * scaledSize;
                                            var destY = dy * scaledSize;
                                            intermediateTile.Mutate(x => x.DrawImage(fallbackTile, new Point(destX, destY), 1f));
                                        }
                                    }
                                }

                                // Place intermediate tile in 512px output
                                if (intermediateFound > 0 || fallbackTile != null)
                                {
                                    var dest512X = ix * BaseTileSize;
                                    var dest512Y = iy * BaseTileSize;
                                    outputTile.Mutate(x => x.DrawImage(intermediateTile, new Point(dest512X, dest512Y), 1f));
                                    tilesFound++;
                                }
                            }
                        }

                        if (tilesFound > 0)
                        {
                            await SaveImageAsync(outputTile, outputPath, ct);
                        }
                    }
                    finally
                    {
                        fallbackTile?.Dispose();
                    }
                }
            });
        }
        else
        {
            // Standard 256px mode
            var zoomDir = Path.Combine(outputDirectory, maxZoom.ToString());

            // Pre-create output directories
            var uniqueXValues = intermediateCoords.Select(c => c.X).Distinct();
            foreach (var x in uniqueXValues)
            {
                Directory.CreateDirectory(Path.Combine(zoomDir, x.ToString()));
            }

            _logger.LogInformation("Creating {Count} stitched 256px tiles at zoom {Zoom}...",
                intermediateCoords.Count, maxZoom);

            await Parallel.ForEachAsync(intermediateCoords, parallelOptions, async (coord, ct) =>
            {
                var (outX, outY) = coord;
                var outputPath = Path.Combine(zoomDir, outX.ToString(), $"{outY}{fileExt}");

                if (!File.Exists(outputPath))
                {
                    // Load fallback tile for this thread if available
                    Image<Rgba32>? fallbackTile = null;
                    if (fallbackTileData != null)
                    {
                        fallbackTile = Image.Load<Rgba32>(fallbackTileData);
                    }

                    try
                    {
                        using var outputTile = new Image<Rgba32>(BaseTileSize, BaseTileSize);
                        var tilesFound = 0;

                        // Load and place tilesPerOutput x tilesPerOutput source tiles
                        for (int dx = 0; dx < tilesPerOutput; dx++)
                        {
                            for (int dy = 0; dy < tilesPerOutput; dy++)
                            {
                                var normX = outX * tilesPerOutput + dx;
                                var normY = outY * tilesPerOutput + dy;
                                var srcX = normX + globalMinX;
                                var srcY = globalMaxY - normY;

                                if (tilesByCoord.TryGetValue((srcX, srcY), out var srcTile))
                                {
                                    using var image = await Image.LoadAsync<Rgba32>(srcTile.FilePath, ct);

                                    if (image.Width != scaledSize || image.Height != scaledSize)
                                    {
                                        image.Mutate(x => x.Resize(scaledSize, scaledSize));
                                    }

                                    var destX = dx * scaledSize;
                                    var destY = dy * scaledSize;
                                    outputTile.Mutate(x => x.DrawImage(image, new Point(destX, destY), 1f));
                                    tilesFound++;
                                }
                                else if (fallbackTile != null)
                                {
                                    // Use fallback for missing tile
                                    var destX = dx * scaledSize;
                                    var destY = dy * scaledSize;
                                    outputTile.Mutate(x => x.DrawImage(fallbackTile, new Point(destX, destY), 1f));
                                }
                            }
                        }

                        if (tilesFound > 0)
                        {
                            await SaveImageAsync(outputTile, outputPath, ct);
                        }
                    }
                    finally
                    {
                        fallbackTile?.Dispose();
                    }
                }
            });
        }

        // Generate parent zoom levels
        for (int zoom = maxZoom - 1; zoom >= 0; zoom--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GenerateZoomLevelAsync(outputDirectory, zoom, fallbackPath, cancellationToken);
        }

        _logger.LogDebug("Completed processing scaled layer with {Levels} zoom levels", maxZoom + 1);
    }

    /// <summary>
    /// Parses tile files from a directory, extracting coordinates from filenames.
    /// Uses the LAST matching coordinate pattern to support CreateMaps format
    /// where cell names may contain brackets (e.g., "Vivec, Arena Canton (0, -11).png").
    /// </summary>
    private List<TileInfo> ParseTileFiles(string directory)
    {
        var tiles = new List<TileInfo>();
        var files = Directory.GetFiles(directory, "*.png", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directory, "*.bmp", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            // Try to find coordinate patterns - use LAST match to handle CreateMaps format
            // where cell names may contain brackets (e.g., "Vivec, Arena Canton (0, -11)")
            var matches = CoordinatePattern1().Matches(fileName);
            Match? match = null;

            if (matches.Count > 0)
            {
                // Use the LAST match (coordinates are always at the end of CreateMaps filenames)
                match = matches[^1];
            }
            else
            {
                // Try alternative pattern (x_y or x-y format)
                var altMatch = CoordinatePattern2().Match(fileName);
                if (altMatch.Success)
                {
                    match = altMatch;
                }
            }

            if (match != null && match.Success)
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
        while (width > BaseTileSize || height > BaseTileSize)
        {
            depth++;
            width = (width + 1) / 2;
            height = (height + 1) / 2;
        }
        return depth;
    }

    /// <summary>
    /// Saves an image in the configured output format.
    /// </summary>
    private async Task SaveImageAsync(Image<Rgba32> image, string outputPath, CancellationToken cancellationToken)
    {
        if (Options.OutputFormat == TileOutputFormat.WebP)
        {
            var encoder = new WebpEncoder
            {
                FileFormat = WebpFileFormatType.Lossless
            };
            await image.SaveAsWebpAsync(outputPath, encoder, cancellationToken);
        }
        else
        {
            await image.SaveAsPngAsync(outputPath, cancellationToken);
        }
    }

    /// <summary>
    /// Copies source tiles to the deepest zoom level with coordinate normalization.
    /// Coordinates are normalized: X offset by minX, Y flipped and offset by maxY.
    /// In 512px mode, stitches 2x2 source tiles into single 512px output tiles.
    /// </summary>
    private async Task CopyTilesToZoomLevelAsync(
        List<TileInfo> tiles,
        string outputDirectory,
        int zoomLevel,
        int minX,
        int maxY,
        CancellationToken cancellationToken)
    {
        var tileSize = Options.TileSize;
        var fileExt = Options.FileExtension;
        var use512 = Options.Use512pxTiles;

        _logger.LogInformation("Copying {Count} tiles to zoom level {Zoom} (parallelism: {Parallelism}, tile size: {Size}px)...",
            tiles.Count, zoomLevel, MaxDegreeOfParallelism, tileSize);

        var zoomDir = Path.Combine(outputDirectory, zoomLevel.ToString());

        if (use512)
        {
            // 512px mode: stitch 2x2 source tiles into single output tiles
            await CopyTilesStitched512Async(tiles, zoomDir, fileExt, minX, maxY, cancellationToken);
        }
        else
        {
            // Standard 256px mode: copy tiles directly
            await CopyTiles256Async(tiles, zoomDir, fileExt, minX, maxY, cancellationToken);
        }

        _logger.LogDebug("Copied tiles to zoom level {Zoom}", zoomLevel);
    }

    /// <summary>
    /// Copies tiles in standard 256px mode with coordinate normalization.
    /// </summary>
    private async Task CopyTiles256Async(
        List<TileInfo> tiles,
        string zoomDir,
        string fileExt,
        int minX,
        int maxY,
        CancellationToken cancellationToken)
    {
        // Pre-create all X directories (using normalized coordinates)
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
            // Normalize coordinates: X offset by minX, Y flipped
            var outputX = tile.X - minX;
            var outputY = maxY - tile.Y;
            var outputPath = Path.Combine(zoomDir, outputX.ToString(), $"{outputY}{fileExt}");

            if (!File.Exists(outputPath))
            {
                using var image = await Image.LoadAsync<Rgba32>(tile.FilePath, ct);

                if (image.Width != BaseTileSize || image.Height != BaseTileSize)
                {
                    image.Mutate(x => x.Resize(BaseTileSize, BaseTileSize));
                }

                await SaveImageAsync(image, outputPath, ct);
            }
        });
    }

    /// <summary>
    /// Copies tiles in 512px mode by stitching 2x2 source tiles together.
    /// Coordinates are normalized: X offset by minX, Y flipped and offset by maxY.
    /// </summary>
    private async Task CopyTilesStitched512Async(
        List<TileInfo> tiles,
        string zoomDir,
        string fileExt,
        int minX,
        int maxY,
        CancellationToken cancellationToken)
    {
        // Build a lookup of tiles by coordinate
        var tilesByCoord = tiles.ToDictionary(
            t => (t.X, t.Y),
            t => t);

        // Calculate normalized output tile coordinates (each output tile covers 2x2 source tiles)
        // First normalize source coords, then divide by 2 for 512px grouping
        var outputCoords = new HashSet<(int X, int Y)>();
        foreach (var tile in tiles)
        {
            // Normalize coordinates first
            var normX = tile.X - minX;
            var normY = maxY - tile.Y;
            // Then group into 512px tiles
            var outX = normX / 2;
            var outY = normY / 2;
            outputCoords.Add((outX, outY));
        }

        // Pre-create output X directories
        var uniqueXValues = outputCoords.Select(c => c.X).Distinct();
        foreach (var x in uniqueXValues)
        {
            Directory.CreateDirectory(Path.Combine(zoomDir, x.ToString()));
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(outputCoords, parallelOptions, async (coord, ct) =>
        {
            var (outX, outY) = coord;
            var outputPath = Path.Combine(zoomDir, outX.ToString(), $"{outY}{fileExt}");

            if (!File.Exists(outputPath))
            {
                using var outputTile = new Image<Rgba32>(512, 512);
                var tilesFound = 0;

                // Load and place 2x2 source tiles
                for (int dx = 0; dx <= 1; dx++)
                {
                    for (int dy = 0; dy <= 1; dy++)
                    {
                        // Convert back to game coordinates to find source tile
                        var normX = outX * 2 + dx;
                        var normY = outY * 2 + dy;
                        var srcX = normX + minX;
                        var srcY = maxY - normY;

                        if (tilesByCoord.TryGetValue((srcX, srcY), out var srcTile))
                        {
                            using var image = await Image.LoadAsync<Rgba32>(srcTile.FilePath, ct);

                            // Resize source tile to 256x256 if needed
                            if (image.Width != BaseTileSize || image.Height != BaseTileSize)
                            {
                                image.Mutate(x => x.Resize(BaseTileSize, BaseTileSize));
                            }

                            // Place at correct position in 512x512 output
                            var destX = dx * BaseTileSize;
                            var destY = dy * BaseTileSize;
                            outputTile.Mutate(x => x.DrawImage(image, new Point(destX, destY), 1f));
                            tilesFound++;
                        }
                    }
                }

                // Only save if at least one source tile was found
                if (tilesFound > 0)
                {
                    await SaveImageAsync(outputTile, outputPath, ct);
                }
            }
        });
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
        var tileSize = Options.TileSize;
        var fileExt = Options.FileExtension;
        var childZoomLevel = zoomLevel + 1;
        var childZoomDir = Path.Combine(outputDirectory, childZoomLevel.ToString());
        var parentZoomDir = Path.Combine(outputDirectory, zoomLevel.ToString());

        if (!Directory.Exists(childZoomDir))
        {
            _logger.LogWarning("Child zoom directory not found: {Dir}", childZoomDir);
            return;
        }

        // Get all child tiles - support both PNG and WebP
        var childXDirs = Directory.GetDirectories(childZoomDir)
            .Select(d => int.Parse(Path.GetFileName(d)))
            .ToList();

        if (childXDirs.Count == 0) return;

        var minChildX = childXDirs.Min();
        var maxChildX = childXDirs.Max();

        // Find Y range (look for files matching current format)
        var allChildYs = new List<int>();
        foreach (var xDir in Directory.GetDirectories(childZoomDir))
        {
            var yFiles = Directory.GetFiles(xDir, "*" + fileExt)
                .Select(f => int.Parse(Path.GetFileNameWithoutExtension(f)));
            allChildYs.AddRange(yFiles);
        }

        if (allChildYs.Count == 0) return;

        var minChildY = allChildYs.Min();
        var maxChildY = allChildYs.Max();

        // Build list of parent tile coordinates to process
        // Use floor division for negative coordinates: Math.Floor(-3/2.0) = -2, not -1
        var parentTileCoords = new List<(int X, int Y)>();
        var minParentX = (int)Math.Floor(minChildX / 2.0);
        var maxParentX = (int)Math.Floor(maxChildX / 2.0);
        var minParentY = (int)Math.Floor(minChildY / 2.0);
        var maxParentY = (int)Math.Floor(maxChildY / 2.0);
        for (int parentX = minParentX; parentX <= maxParentX; parentX++)
        {
            for (int parentY = minParentY; parentY <= maxParentY; parentY++)
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
            fallbackImage.Mutate(x => x.Resize(tileSize / 2, tileSize / 2));
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
            var parentPath = Path.Combine(parentZoomDir, parentX.ToString(), $"{parentY}{fileExt}");
            if (!File.Exists(parentPath))
            {
                using var parentTile = new Image<Rgba32>(tileSize, tileSize);

                // Load fallback tile for this thread if available
                Image<Rgba32>? fallbackTile = null;
                if (fallbackTileData != null)
                {
                    fallbackTile = Image.Load<Rgba32>(fallbackTileData);
                }

                try
                {
                    // Combine 4 child tiles
                    // Tiles are stored with normalized coordinates (Y already flipped)
                    for (int dx = 0; dx <= 1; dx++)
                    {
                        for (int dy = 0; dy <= 1; dy++)
                        {
                            var childX = parentX * 2 + dx;
                            var childY = parentY * 2 + dy;
                            var childPath = Path.Combine(childZoomDir, childX.ToString(), $"{childY}{fileExt}");

                            // Calculate position in parent tile
                            var destX = dx * (tileSize / 2);
                            var destY = dy * (tileSize / 2);

                            if (File.Exists(childPath))
                            {
                                using var childTile = await Image.LoadAsync<Rgba32>(childPath, ct);

                                // Resize child tile to half size
                                childTile.Mutate(x => x.Resize(tileSize / 2, tileSize / 2));

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
                        await SaveImageAsync(parentTile, parentPath, ct);
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

    /// <summary>
    /// Finds a fallback image in the directory, checking multiple formats.
    /// </summary>
    private string? FindFallbackImage(string directory)
    {
        var extensions = new[] { ".png", ".webp", ".bmp" };
        foreach (var ext in extensions)
        {
            var path = Path.Combine(directory, "fallback" + ext);
            if (File.Exists(path))
            {
                _logger.LogDebug("Found fallback image: {Path}", path);
                return path;
            }
        }
        _logger.LogDebug("No fallback image found in {Directory}", directory);
        return null;
    }

    // Pattern: "(x,y)" - Morrowind CreateMaps format
    [GeneratedRegex(@"\((?<x>-?\d+),\s*(?<y>-?\d+)\)")]
    private static partial Regex CoordinatePattern1();

    // Pattern: "x_y" or "x-y" - alternative formats
    [GeneratedRegex(@"^(?<x>-?\d+)[_-](?<y>-?\d+)$")]
    private static partial Regex CoordinatePattern2();
}
