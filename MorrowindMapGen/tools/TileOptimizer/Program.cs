using System.Collections.Concurrent;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

const string InputDir = @"D:\Repositories\Github\Morrowind-MapBuilder\tilesets\settlements_sliced";
const string OutputDir = @"D:\Repositories\Github\Morrowind-MapBuilder\tilesets\settlements";
const string WaterTileName = "(-334,-164).png";
const string FallbackName = "fallback.png";
//const int TargetSize = 256;
const bool IsLandscape = false;

Console.WriteLine("TileOptimizer - De-duplicate water tiles and upscale to 256px");
Console.WriteLine("==============================================================");

// Create output directory
Directory.CreateDirectory(OutputDir);


// Step 1: Create water reference from corner tile
byte[] waterHash;
if (IsLandscape)
{
    Console.WriteLine($"\nStep 1: Creating water reference from {WaterTileName}...");
    var waterTilePath = Path.Combine(InputDir, WaterTileName);
    if (!File.Exists(waterTilePath))
    {
        Console.WriteLine($"ERROR: Water reference tile not found: {waterTilePath}");
        return 1;
    }
    using (var waterImage = Image.Load<Rgba32>(waterTilePath))
    {
        waterHash = GetPixelHash(waterImage);
        // Upscale and save as fallback
        var fallbackPath = Path.Combine(OutputDir, FallbackName);
        waterImage.SaveAsPng(fallbackPath);
        Console.WriteLine($"  Saved fallback tile: {fallbackPath}");
        Console.WriteLine($"  Water hash: {Convert.ToHexString(waterHash)[..16]}...");
    }
}

// Step 2: Process all tiles
Console.WriteLine("\nStep 2: Processing tiles...");
var inputFiles = Directory.GetFiles(InputDir, "(*).png");
Console.WriteLine($"  Found {inputFiles.Length:N0} tiles to process");

int processed = 0;
int skipped = 0;
int errors = 0;
var processedCount = 0;
var lockObj = new object();

Parallel.ForEach(inputFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, tilePath =>
{
    try
    {
        var fileName = Path.GetFileName(tilePath);

        using var image = Image.Load<Rgba32>(tilePath);
        var tileHash = GetPixelHash(image);

        // Compare with water hash
        if (IsLandscape && tileHash.SequenceEqual(waterHash))
        {
            Interlocked.Increment(ref skipped);
        }
        else
        {
            //Upscale and save
            //image.Mutate(x => x.Resize(TargetSize, TargetSize));
            var outputPath = Path.Combine(OutputDir, fileName);
            image.SaveAsPng(outputPath);
            Interlocked.Increment(ref processed);
        }

        // Progress reporting
        var count = Interlocked.Increment(ref processedCount);
        if (count % 5000 == 0)
        {
            lock (lockObj)
            {
                Console.WriteLine($"  Progress: {count:N0}/{inputFiles.Length:N0} ({100.0 * count / inputFiles.Length:F1}%)");
            }
        }
    }
    catch (Exception ex)
    {
        Interlocked.Increment(ref errors);
        lock (lockObj)
        {
            Console.WriteLine($"  Error processing {Path.GetFileName(tilePath)}: {ex.Message}");
        }
    }
});

// Step 3: Report statistics
Console.WriteLine("\nStep 3: Results");
Console.WriteLine("===============");
Console.WriteLine($"  Total tiles scanned:  {inputFiles.Length:N0}");
Console.WriteLine($"  Water tiles skipped:  {skipped:N0}");
Console.WriteLine($"  Unique tiles output:  {processed:N0}");
Console.WriteLine($"  Errors:               {errors:N0}");
Console.WriteLine($"  Output files:         {processed + 1:N0} (includes fallback_water.png)");

var reductionPercent = 100.0 * skipped / inputFiles.Length;
Console.WriteLine($"\n  Storage reduction:    {reductionPercent:F1}% fewer files");

Console.WriteLine($"\n  Output directory: {OutputDir}");
Console.WriteLine("\nDone!");

return 0;

static byte[] GetPixelHash(Image<Rgba32> image)
{
    var pixels = new byte[image.Width * image.Height * 4];
    image.CopyPixelDataTo(pixels);
    return SHA256.HashData(pixels);
}
