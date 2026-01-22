namespace MorrowindMapGen.Core.MapGeneration;

/// <summary>
/// Output format for generated tiles.
/// </summary>
public enum TileOutputFormat
{
    /// <summary>
    /// PNG format (lossless, larger file size).
    /// </summary>
    Png,

    /// <summary>
    /// WebP format (lossless, smaller file size).
    /// </summary>
    WebP
}

/// <summary>
/// Options for tile processing.
/// </summary>
public class TileProcessingOptions
{
    /// <summary>
    /// Output format for generated tiles.
    /// </summary>
    public TileOutputFormat OutputFormat { get; set; } = TileOutputFormat.WebP;

    /// <summary>
    /// When true, generates 512px tiles instead of 256px tiles.
    /// This reduces the number of HTTP requests by 4x but increases individual file sizes.
    /// </summary>
    public bool Use512pxTiles { get; set; }

    /// <summary>
    /// Gets the effective tile size based on options.
    /// </summary>
    public int TileSize => Use512pxTiles ? 512 : 256;

    /// <summary>
    /// Gets the file extension for the output format.
    /// </summary>
    public string FileExtension => OutputFormat == TileOutputFormat.WebP ? ".webp" : ".png";
}
