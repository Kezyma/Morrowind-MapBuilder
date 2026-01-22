using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace MorrowindMapGen.Core.MapGeneration;

/// <summary>
/// Information about how to scale a layer's source tiles.
/// </summary>
public class LayerScaleInfo
{
    /// <summary>
    /// Original size of source tiles in pixels.
    /// </summary>
    public int SourceTileSize { get; set; }

    /// <summary>
    /// Target size after scaling to nearest power of 2 (16, 32, 64, 128, or 256).
    /// </summary>
    public int ScaledTileSize { get; set; }

    /// <summary>
    /// Number of source tiles to stitch per output tile dimension.
    /// For example, if scaled size is 64px and output is 256px, this is 4 (4x4 grid).
    /// </summary>
    public int TilesPerOutputTile { get; set; }

    /// <summary>
    /// Zoom level difference from the base layer's max zoom.
    /// For example, if scaled tiles are 64px (vs 256px base), this is 2 (2^2 = 4).
    /// </summary>
    public int ZoomLevelDifference { get; set; }
}

/// <summary>
/// Analyzes source tiles and calculates optimal scaling parameters for web display.
/// </summary>
public class LayerTileAnalyzer
{
    private readonly ILogger<LayerTileAnalyzer> _logger;
    private const int BaseTileSize = 256;

    public LayerTileAnalyzer(ILogger<LayerTileAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes source tiles to determine their size and calculate optimal scaling.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing source tiles.</param>
    /// <returns>Scaling information for the layer, or null if no valid tiles found.</returns>
    public LayerScaleInfo? AnalyzeSourceTiles(string sourceDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            _logger.LogWarning("Source directory does not exist: {Path}", sourceDirectory);
            return null;
        }

        // Find the first valid image file to determine tile size
        var imageFiles = Directory.GetFiles(sourceDirectory, "*.png", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(sourceDirectory, "*.bmp", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(sourceDirectory, "*.webp", SearchOption.AllDirectories));

        foreach (var file in imageFiles)
        {
            try
            {
                // Skip fallback images
                if (Path.GetFileNameWithoutExtension(file).Equals("fallback", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using var image = Image.Load(file);
                var sourceSize = Math.Max(image.Width, image.Height);

                _logger.LogDebug("Detected source tile size: {Size}px from {File}", sourceSize, Path.GetFileName(file));

                return CalculateScaling(sourceSize);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to read image {File}: {Error}", file, ex.Message);
                continue;
            }
        }

        _logger.LogWarning("No valid image files found in {Path}", sourceDirectory);
        return null;
    }

    /// <summary>
    /// Calculates scaling parameters for a given source tile size.
    /// </summary>
    /// <param name="sourceTileSize">Size of source tiles in pixels.</param>
    /// <returns>Scaling information.</returns>
    public LayerScaleInfo CalculateScaling(int sourceTileSize)
    {
        var scaledSize = GetNearestPowerOf2(sourceTileSize);
        var tilesPerOutput = BaseTileSize / scaledSize;
        var zoomDiff = CalculateZoomDifference(scaledSize);

        _logger.LogDebug(
            "Scaling: {Source}px -> {Scaled}px, {TilesPerOutput}x{TilesPerOutput2} per 256px tile, zoom diff: {ZoomDiff}",
            sourceTileSize, scaledSize, tilesPerOutput, tilesPerOutput, zoomDiff);

        return new LayerScaleInfo
        {
            SourceTileSize = sourceTileSize,
            ScaledTileSize = scaledSize,
            TilesPerOutputTile = tilesPerOutput,
            ZoomLevelDifference = zoomDiff
        };
    }

    /// <summary>
    /// Gets the nearest power of 2 that is greater than or equal to the input size.
    /// Valid outputs are 16, 32, 64, 128, or 256.
    /// For example: 41 -> 64, 64 -> 64, 65 -> 128, 256 -> 256
    /// </summary>
    /// <param name="size">Input size in pixels.</param>
    /// <returns>Nearest power of 2 >= size (clamped between 16 and 256).</returns>
    public static int GetNearestPowerOf2(int size)
    {
        // Find the smallest power of 2 >= size
        int power = 16; // Minimum
        while (power < size && power < 256)
        {
            power *= 2;
        }

        // Clamp between 16 and 256
        return Math.Clamp(power, 16, 256);
    }

    /// <summary>
    /// Calculates how many zoom levels less this layer has compared to the base layer.
    /// </summary>
    /// <param name="scaledSize">The scaled tile size (power of 2).</param>
    /// <param name="baseTileSize">The base tile size (default 256).</param>
    /// <returns>Number of zoom levels difference.</returns>
    public static int CalculateZoomDifference(int scaledSize, int baseTileSize = 256)
    {
        // Each halving of tile size = 1 zoom level less
        // 256 -> 0, 128 -> 1, 64 -> 2, 32 -> 3, 16 -> 4
        int diff = 0;
        int current = baseTileSize;

        while (current > scaledSize && current > 16)
        {
            current /= 2;
            diff++;
        }

        return diff;
    }

    /// <summary>
    /// Determines if a layer needs special processing (tile stitching).
    /// </summary>
    /// <param name="scaleInfo">Scaling information for the layer.</param>
    /// <returns>True if tiles need to be stitched together.</returns>
    public static bool NeedsStitching(LayerScaleInfo scaleInfo)
    {
        return scaleInfo.ScaledTileSize < BaseTileSize;
    }
}
