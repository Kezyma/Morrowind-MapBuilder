using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

// Configuration
const string inputPath = @"D:\Repositories\Github\Morrowind-MapBuilder\GM_Settlements.png";
const string outputDir = @"D:\Repositories\Github\Morrowind-MapBuilder\worldmap_settlements";

const int minX = -334;
const int maxX = 333;
const int minY = -164;
const int maxY = 66;

int cellsX = maxX - minX + 1;  // 668
int cellsY = maxY - minY + 1;  // 231

Console.WriteLine($"Loading image: {inputPath}");
using var image = Image.Load<Rgba32>(inputPath);

Console.WriteLine($"Image size: {image.Width} x {image.Height}");
Console.WriteLine($"Cell range: X[{minX} to {maxX}] ({cellsX} cells), Y[{minY} to {maxY}] ({cellsY} cells)");

double pixelsPerCellX = (double)image.Width / cellsX;
double pixelsPerCellY = (double)image.Height / cellsY;

Console.WriteLine($"Pixels per cell: {pixelsPerCellX:F2} x {pixelsPerCellY:F2}");

Directory.CreateDirectory(outputDir);

int totalTiles = cellsX * cellsY;
int processed = 0;
object lockObj = new();

Console.WriteLine($"Slicing into {totalTiles} tiles using parallel processing...");

// Process in parallel for speed
Parallel.For(minX, maxX + 1, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, cellX =>
{
    for (int cellY = minY; cellY <= maxY; cellY++)
    {
        // Calculate image coordinates
        // Image (0,0) is top-left
        // Map bottom-left is (minX, minY), top-right is (maxX, maxY)
        int imgX = (int)((cellX - minX) * pixelsPerCellX);
        int imgY = (int)((maxY - cellY) * pixelsPerCellY);  // Y inverted

        int tileWidth = (int)Math.Ceiling(pixelsPerCellX);
        int tileHeight = (int)Math.Ceiling(pixelsPerCellY);

        // Clamp to image bounds
        if (imgX + tileWidth > image.Width) tileWidth = image.Width - imgX;
        if (imgY + tileHeight > image.Height) tileHeight = image.Height - imgY;

        if (tileWidth > 0 && tileHeight > 0 && imgX >= 0 && imgY >= 0)
        {
            var rect = new Rectangle(imgX, imgY, tileWidth, tileHeight);

            // Clone the region - need to lock since image operations aren't thread-safe
            Image<Rgba32> tile;
            lock (image)
            {
                tile = image.Clone(ctx => ctx.Crop(rect));
            }

            var skip = true;
            for (int y = 0; y < tile.Height; y++)
                for (int x = 0; x < tile.Width; x++)
                {
                    Rgba32 pixel = tile[x, y];
                    if (pixel.A > 0)
                    {
                        skip = false;
                        break;
                    }
                }

            if (!skip)
            {
                string tilePath = Path.Combine(outputDir, $"({cellX},{cellY}).png");
                tile.SaveAsPng(tilePath);
            }
            tile.Dispose();
        }

        int current = Interlocked.Increment(ref processed);
        if (current % 5000 == 0)
        {
            int pct = (int)((double)current / totalTiles * 100);
            Console.WriteLine($"Progress: {current} / {totalTiles} ({pct}%)");
        }
    }
});

Console.WriteLine($"Done! Created {processed} tiles in: {outputDir}");
