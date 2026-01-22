using Morroweb.Net.Services;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Morroweb.Net.MapGenerator
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                AllocConsole();

                var mwPath = args[0];
                var outPath = args[1];
                var bigMap = args.Any(x => x.ToLower() == "bigmap");
                var coords = args.Any(x => x.ToLower() == "markers");
                var compress = args.Any(x => x.ToLower() == "compress");
                var mapPath = Path.Combine(mwPath, "Maps");
                if (args.Length > 2 && Directory.Exists(args[2])) mapPath = args[2];

                Console.WriteLine("Generating Map");
                Console.WriteLine($"Maps Path: {mapPath}");
                Console.WriteLine($"Output Path: {outPath}");

                Console.WriteLine("Generating Tiles");
                var espExt = new EspExtractorService();
                var mapGen = new TileGeneratorService(espExt);
                mapGen.GenerateTiles(mapPath, outPath, compress);

                if (bigMap == true)
                {
                    Console.WriteLine("Generating Full Size Map");
                    mapGen.StitchTiles(mapPath, outPath, compress);
                }

                if (coords == true)
                {
                    Console.WriteLine("Exporting Coordinates");
                    mapGen.GenerateCoordinates(mwPath, outPath);
                }

                Console.WriteLine("Generating Demo");
                mapGen.GenerateDemo(outPath);

                Console.WriteLine("Complete");
            }
            else
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new MapBuilder());
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}