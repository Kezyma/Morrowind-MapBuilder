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
    private const int TileSize = 256;
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

        var maxZoomDir = Path.Combine(tilesDirectory, metadata.MaxZoom.ToString());

        if (!Directory.Exists(maxZoomDir))
        {
            throw new DirectoryNotFoundException($"Max zoom directory not found: {maxZoomDir}");
        }

        var widthPixels = metadata.WidthInPixels;
        var heightPixels = metadata.HeightInPixels;

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

        var scaledTileSize = (int)(TileSize * scale);

        // Find all tile directories
        var xDirs = Directory.GetDirectories(maxZoomDir)
            .Select(d => int.Parse(Path.GetFileName(d)))
            .OrderBy(x => x)
            .ToList();

        var tilesProcessed = 0;
        var totalTiles = 0;

        // Count total tiles first
        foreach (var x in xDirs)
        {
            var xDir = Path.Combine(maxZoomDir, x.ToString());
            totalTiles += Directory.GetFiles(xDir, "*.png").Length;
        }

        _logger.LogInformation("Stitching {Total} tiles...", totalTiles);

        // Draw each tile
        foreach (var x in xDirs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var xDir = Path.Combine(maxZoomDir, x.ToString());
            var yFiles = Directory.GetFiles(xDir, "*.png");

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
                var destX = x * scaledTileSize;
                var destY = y * scaledTileSize;

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
