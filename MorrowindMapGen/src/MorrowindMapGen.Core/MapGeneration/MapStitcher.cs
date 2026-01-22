using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MorrowindMapGen.Core.MapGeneration;

/// <summary>
/// Stitches map tiles into a single large image.
/// </summary>
public class MapStitcher
{
    private readonly ILogger<MapStitcher> _logger;
    private const int MaxImageDimension = 23170; // Practical limit for large images

    public MapStitcher(ILogger<MapStitcher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Stitches tiles from the deepest zoom level into a single image.
    /// </summary>
    /// <param name="tilesDirectory">Directory containing the zoom pyramid.</param>
    /// <param name="metadata">Map metadata with bounds information.</param>
    /// <param name="outputPath">Path for the output image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StitchMapAsync(
        string tilesDirectory,
        MapMetadata metadata,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stitching full map image...");

        // Tiles are always in tiles/base/ subdirectory
        var maxZoomDir = Path.Combine(tilesDirectory, "tiles", "base", metadata.MaxZoom.ToString());

        if (!Directory.Exists(maxZoomDir))
        {
            throw new DirectoryNotFoundException($"Max zoom directory not found: {maxZoomDir}");
        }

        var tileSize = metadata.TileSize;

        // Find all tile directories and calculate bounds from actual tiles
        // This ensures correct dimensions even when layers have different bounds than base map
        var xDirs = Directory.GetDirectories(maxZoomDir)
            .Select(d => int.Parse(Path.GetFileName(d)))
            .OrderBy(x => x)
            .ToList();

        if (xDirs.Count == 0)
        {
            _logger.LogWarning("No tile directories found in {Dir}", maxZoomDir);
            return;
        }

        // Calculate actual tile bounds from the base map tiles
        var minTileX = xDirs.Min();
        var maxTileX = xDirs.Max();

        // Find Y range by scanning all directories
        var allYValues = new List<int>();
        var totalTiles = 0;
        foreach (var x in xDirs)
        {
            var xDir = Path.Combine(maxZoomDir, x.ToString());
            var pngFiles = Directory.GetFiles(xDir, "*.png");
            var webpFiles = Directory.GetFiles(xDir, "*.webp");
            totalTiles += pngFiles.Length + webpFiles.Length;

            var yValues = pngFiles.Concat(webpFiles)
                .Select(f => int.Parse(Path.GetFileNameWithoutExtension(f)));
            allYValues.AddRange(yValues);
        }

        if (totalTiles == 0)
        {
            _logger.LogWarning("No tiles found in {Dir}", maxZoomDir);
            return;
        }

        var minTileY = allYValues.Min();
        var maxTileY = allYValues.Max();

        // Calculate dimensions from actual tile bounds (not metadata global bounds)
        var widthInTiles = maxTileX - minTileX + 1;
        var heightInTiles = maxTileY - minTileY + 1;
        var widthPixels = widthInTiles * tileSize;
        var heightPixels = heightInTiles * tileSize;

        _logger.LogInformation("Base map bounds: X=[{MinX}, {MaxX}], Y=[{MinY}, {MaxY}] ({Width}x{Height} tiles)",
            minTileX, maxTileX, minTileY, maxTileY, widthInTiles, heightInTiles);

        // Check if we need to scale down
        var scale = 1.0;
        if (widthPixels > MaxImageDimension || heightPixels > MaxImageDimension)
        {
            scale = Math.Min(
                (double)MaxImageDimension / widthPixels,
                (double)MaxImageDimension / heightPixels);

            _logger.LogWarning(
                "Map size ({Width}x{Height}) exceeds maximum. Scaling to {Scale:P0}",
                widthPixels, heightPixels, scale);

            widthPixels = (int)(widthPixels * scale);
            heightPixels = (int)(heightPixels * scale);
        }

        _logger.LogInformation("Creating {Width}x{Height} pixel image...", widthPixels, heightPixels);

        // Create the output image
        using var image = new Image<Rgba32>(widthPixels, heightPixels);

        var scaledTileSize = (int)(tileSize * scale);
        var tilesProcessed = 0;

        _logger.LogInformation("Stitching {Total} tiles...", totalTiles);

        // Draw each tile
        // Tiles are stored with normalized coordinates: X starts at 0, Y is already flipped
        foreach (var x in xDirs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var xDir = Path.Combine(maxZoomDir, x.ToString());

            // Get all tile files (png or webp)
            var yFiles = Directory.GetFiles(xDir, "*.png")
                .Concat(Directory.GetFiles(xDir, "*.webp"))
                .ToList();

            foreach (var yFile in yFiles)
            {
                var y = int.Parse(Path.GetFileNameWithoutExtension(yFile));

                using var tile = await Image.LoadAsync<Rgba32>(yFile, cancellationToken);

                // Scale tile if needed
                if (scale < 1.0)
                {
                    tile.Mutate(ctx => ctx.Resize(scaledTileSize, scaledTileSize));
                }

                // Calculate position in output image
                // Offset by minimum tile coordinates to handle non-zero starting positions
                var destX = (x - minTileX) * scaledTileSize;
                var destY = (y - minTileY) * scaledTileSize;

                // Draw tile onto output image
                image.Mutate(ctx => ctx.DrawImage(tile, new Point(destX, destY), 1f));

                tilesProcessed++;

                if (tilesProcessed % 100 == 0)
                {
                    _logger.LogDebug("Progress: {Processed}/{Total} tiles", tilesProcessed, totalTiles);
                }
            }
        }

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // Save the image
        _logger.LogInformation("Saving map to {Path}...", outputPath);
        await image.SaveAsPngAsync(outputPath, cancellationToken);

        _logger.LogInformation("Map image saved: {Width}x{Height} pixels", image.Width, image.Height);
    }
}
