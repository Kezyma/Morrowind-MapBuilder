using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Converts PNG images to WebP format, preserving folder structure.
/// </summary>
public class PngToWebpConverter
{
    private readonly ILogger<PngToWebpConverter> _logger;

    public PngToWebpConverter(ILogger<PngToWebpConverter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Converts all PNG files in a directory to WebP format.
    /// </summary>
    /// <param name="inputDirectory">Source directory containing PNG files.</param>
    /// <param name="outputDirectory">Destination directory for WebP files.</param>
    /// <param name="lossless">Use lossless compression (default: true).</param>
    /// <param name="quality">Quality level 0-100 for lossy compression (default: 90).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of files converted.</returns>
    public async Task<int> ConvertDirectoryAsync(
        string inputDirectory,
        string outputDirectory,
        bool lossless = true,
        int quality = 90,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(inputDirectory))
        {
            throw new DirectoryNotFoundException($"Input directory not found: {inputDirectory}");
        }

        _logger.LogInformation("Converting PNG files from {Input} to {Output}", inputDirectory, outputDirectory);
        _logger.LogInformation("Settings: Lossless={Lossless}, Quality={Quality}", lossless, quality);

        // Find all PNG files recursively
        var pngFiles = Directory.GetFiles(inputDirectory, "*.png", SearchOption.AllDirectories);
        _logger.LogInformation("Found {Count} PNG files to convert", pngFiles.Length);

        if (pngFiles.Length == 0)
        {
            _logger.LogWarning("No PNG files found in {Directory}", inputDirectory);
            return 0;
        }

        Directory.CreateDirectory(outputDirectory);

        var encoder = new WebpEncoder
        {
            FileFormat = lossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
            Quality = quality
        };

        int converted = 0;
        int failed = 0;

        Parallel.ForEach(pngFiles, pngPath =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Calculate relative path from input directory
                var relativePath = Path.GetRelativePath(inputDirectory, pngPath);

                // Change extension to .webp
                var webpRelativePath = Path.ChangeExtension(relativePath, ".webp");
                var webpPath = Path.Combine(outputDirectory, webpRelativePath);

                if (!File.Exists(webpPath))
                {
                    // Create output directory structure
                    var webpDir = Path.GetDirectoryName(webpPath);
                    if (!string.IsNullOrEmpty(webpDir))
                    {
                        Directory.CreateDirectory(webpDir);
                    }

                    // Load PNG and save as WebP
                    using var image = Image.LoadAsync(pngPath, cancellationToken).Result;
                    image.SaveAsync(webpPath, encoder, cancellationToken).Wait();
                }

                converted++;

                if (converted % 100 == 0)
                {
                    _logger.LogInformation("Progress: {Converted}/{Total} files converted", converted, pngFiles.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert {File}", pngPath);
                failed++;
            }
        });

        _logger.LogInformation("Conversion complete: {Converted} succeeded, {Failed} failed", converted, failed);

        return converted;
    }
}
