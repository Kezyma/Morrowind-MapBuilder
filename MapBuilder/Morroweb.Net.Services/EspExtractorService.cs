using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Morroweb.Net.Services
{
    public class EspExtractorService
    {
        public EspExtractorService() { }
        private const string Tes3convUrl = "https://github.com/Greatness7/tes3conv/releases/download/v0.1.0/windows-latest.zip";
        public void ExtractEsps(string dataDir, string outputDir, string[] modList = null, bool cleanup = false)
        {
            Console.WriteLine("Extracting Esps");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            // Get tes3conv.
            var tes3convDir = Path.GetFullPath("tes3conv");
            if (!Directory.Exists(tes3convDir)) Directory.CreateDirectory(tes3convDir);
            var tes3convExe = Path.Combine(tes3convDir, "tes3conv.exe");
            var tes3convZip = Path.Combine(tes3convDir, "tes3conv.zip");
            if (!File.Exists(tes3convExe))
            {
                Console.WriteLine("tes3conv not present, downloading.");
                var client = new WebClient();
                client.DownloadFile(Tes3convUrl, tes3convZip);
                ZipFile.ExtractToDirectory(tes3convZip, tes3convDir, true);
                Console.WriteLine("tes3conv downloaded.");
            }

            // Find all the esp/esm files and convert to json.
            Console.WriteLine("Converting esp/esm files to json.");
            var dataFiles = Directory.GetFiles(dataDir);
            var espFiles = dataFiles.Where(x => x.ToLower().EndsWith(".esp") || x.ToLower().EndsWith(".esm")).ToList();
            var fileNames = new List<string>();
            foreach (var file in espFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (modList == null || modList.Contains(fileName))
                {
                    var jsonDest = Path.Combine(outputDir, $"{fileName}.json");
                    var command = $"\"{tes3convExe}\" \"{file}\" \"{jsonDest}\"";
                    Process.Start(command).WaitForExit();
                    Console.WriteLine($"Converted {fileName} to json.");
                    fileNames.Add(fileName);
                }
            }
            Console.WriteLine("Esp extraction complete.");

            if (cleanup)
            {
                Directory.Delete(tes3convDir, true);
            }
        }
    }
}
