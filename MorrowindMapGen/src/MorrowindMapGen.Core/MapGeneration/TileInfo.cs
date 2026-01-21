namespace MorrowindMapGen.Core.MapGeneration;

/// <summary>
/// Information about a single map tile.
/// </summary>
public class TileInfo
{
    /// <summary>
    /// The X coordinate of the tile in the original grid.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// The Y coordinate of the tile in the original grid.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// The full path to the tile image file.
    /// </summary>
    public required string FilePath { get; set; }

    public override string ToString() => $"Tile ({X}, {Y})";
}

/// <summary>
/// Metadata about the generated map tiles.
/// </summary>
public class MapMetadata
{
    /// <summary>
    /// Minimum X coordinate in the tile grid.
    /// </summary>
    public int MinX { get; set; }

    /// <summary>
    /// Maximum X coordinate in the tile grid.
    /// </summary>
    public int MaxX { get; set; }

    /// <summary>
    /// Minimum Y coordinate in the tile grid.
    /// </summary>
    public int MinY { get; set; }

    /// <summary>
    /// Maximum Y coordinate in the tile grid.
    /// </summary>
    public int MaxY { get; set; }

    /// <summary>
    /// Width of the map in tiles.
    /// </summary>
    public int WidthInTiles => MaxX - MinX + 1;

    /// <summary>
    /// Height of the map in tiles.
    /// </summary>
    public int HeightInTiles => MaxY - MinY + 1;

    /// <summary>
    /// Width of the map in pixels (at maximum zoom).
    /// </summary>
    public int WidthInPixels => WidthInTiles * TileSize;

    /// <summary>
    /// Height of the map in pixels (at maximum zoom).
    /// </summary>
    public int HeightInPixels => HeightInTiles * TileSize;

    /// <summary>
    /// Size of each tile in pixels.
    /// </summary>
    public int TileSize { get; set; } = 256;

    /// <summary>
    /// Maximum zoom level (deepest layer).
    /// </summary>
    public int MaxZoom { get; set; }

    /// <summary>
    /// Total number of tiles at the maximum zoom level.
    /// </summary>
    public int TotalTiles { get; set; }
}
