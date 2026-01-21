using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MorrowindMapGen.Core;
using MorrowindMapGen.Core.Configuration;
using MorrowindMapGen.Core.MapGeneration;
using MorrowindMapGen.Core.Tools;

namespace MorrowindMapGen.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();

        return command switch
        {
            "generate" => await RunGenerateAsync(args[1..]),
            "validate" => RunValidate(args[1..]),
            "convert-webp" => await RunConvertWebpAsync(args[1..]),
            _ => HandleUnknownCommand(command)
        };
    }

    static void PrintUsage()
    {
        Console.WriteLine("""
            Morrowind Map Generator (mwmapgen)

            Generate interactive web-based maps from The Elder Scrolls III: Morrowind.
            Supports both classic Morrowind.ini and OpenMW openmw.cfg configurations.

            Usage:
              mwmapgen <command> [options]

            Commands:
              generate      Generate a map from a Morrowind installation
              validate      Validate a configuration file
              convert-webp  Convert PNG files to WebP format

            Generate Options:
              -c, --config <path>     Path to Morrowind.ini or openmw.cfg
              -o, --output <path>     Output directory for generated files (required)
              -b, --bigmap            Generate a full-size stitched map image
              -m, --markers           Generate markers.json with cell and door locations
              -t, --tools-dir <path>  Custom directory for external tools
              -v, --verbose           Enable verbose output
              --morrowind             Auto-detect Morrowind.ini from Windows registry
              --openmw                Auto-detect openmw.cfg from Documents\My Games\OpenMW
              --underlayer <name> <path>  Add a tile layer below the main map (can repeat)
              --overlayer <name> <path>   Add a tile layer above the main map (can repeat)

            Validate Options:
              -c, --config <path>     Path to Morrowind.ini or openmw.cfg
              --morrowind             Auto-detect Morrowind.ini from Windows registry
              --openmw                Auto-detect openmw.cfg from Documents\My Games\OpenMW

            Convert-WebP Options:
              -i, --input <path>      Input directory containing PNG files (required)
              -o, --output <path>     Output directory for WebP files (required)
              --lossy                 Use lossy compression (default: lossless)
              -q, --quality <0-100>   Quality level for lossy compression (default: 90)
              -v, --verbose           Enable verbose output

            Layer Options:
              Layer folders should contain (x,y).png tiles matching the OpenMW coordinate format.
              Optional fallback.png in the layer folder will be used for missing tiles.

            Examples:
              mwmapgen generate -c "C:/Games/Morrowind/Morrowind.ini" -o "./map"
              mwmapgen generate --morrowind -o "./map" --markers
              mwmapgen generate --openmw -o "./map" --markers --bigmap
              mwmapgen generate --openmw -o "./map" --underlayer worldmap "./worldmap_tiles"
              mwmapgen generate --openmw -o "./map" --overlayer grid "./grid_tiles"
              mwmapgen validate --morrowind
              mwmapgen convert-webp -i "./tiles" -o "./tiles-webp"
              mwmapgen convert-webp -i "./tiles" -o "./tiles-webp" --lossy -q 85
            """);
    }

    static int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        Console.Error.WriteLine("Use --help for usage information.");
        return 1;
    }

    /// <summary>
    /// Attempts to find Morrowind.ini from the Windows registry.
    /// </summary>
    static string? FindMorrowindIniFromRegistry()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.Error.WriteLine("--morrowind option is only available on Windows");
            return null;
        }

        // Try various registry paths where Morrowind might be registered
        string[] registryPaths = [
            @"SOFTWARE\Bethesda Softworks\Morrowind",
            @"SOFTWARE\WOW6432Node\Bethesda Softworks\Morrowind",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Morrowind",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Morrowind",
            @"SOFTWARE\GOG.com\Games\1440163901",  // GOG Morrowind
            @"SOFTWARE\WOW6432Node\GOG.com\Games\1440163901"
        ];

        foreach (var regPath in registryPaths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                if (key == null) continue;

                // Try different value names
                var installPath = key.GetValue("Installed Path") as string
                    ?? key.GetValue("InstallLocation") as string
                    ?? key.GetValue("path") as string
                    ?? key.GetValue("INSTALLDIR") as string;

                if (!string.IsNullOrEmpty(installPath))
                {
                    var iniPath = Path.Combine(installPath, "Morrowind.ini");
                    if (File.Exists(iniPath))
                    {
                        return iniPath;
                    }
                }
            }
            catch
            {
                // Ignore registry access errors
            }
        }

        // Also try HKEY_CURRENT_USER
        foreach (var regPath in registryPaths)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(regPath);
                if (key == null) continue;

                var installPath = key.GetValue("Installed Path") as string
                    ?? key.GetValue("InstallLocation") as string;

                if (!string.IsNullOrEmpty(installPath))
                {
                    var iniPath = Path.Combine(installPath, "Morrowind.ini");
                    if (File.Exists(iniPath))
                    {
                        return iniPath;
                    }
                }
            }
            catch
            {
                // Ignore registry access errors
            }
        }

        Console.Error.WriteLine("Could not find Morrowind installation in Windows registry.");
        Console.Error.WriteLine("Please specify the config path manually with --config.");
        return null;
    }

    /// <summary>
    /// Attempts to find openmw.cfg from the default OpenMW location.
    /// </summary>
    static string? FindOpenMWConfig()
    {
        // OpenMW stores config in Documents\My Games\OpenMW on Windows
        // or ~/.config/openmw on Linux
        string[] possiblePaths;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            possiblePaths = [
                Path.Combine(documentsPath, "My Games", "OpenMW", "openmw.cfg"),
                Path.Combine(documentsPath, "my games", "openmw", "openmw.cfg")
            ];
        }
        else
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            possiblePaths = [
                Path.Combine(homePath, ".config", "openmw", "openmw.cfg"),
                Path.Combine(homePath, ".local", "share", "openmw", "openmw.cfg")
            ];
        }

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        Console.Error.WriteLine("Could not find openmw.cfg in default locations:");
        foreach (var path in possiblePaths)
        {
            Console.Error.WriteLine($"  - {path}");
        }
        Console.Error.WriteLine("Please specify the config path manually with --config.");
        return null;
    }

    static async Task<int> RunGenerateAsync(string[] args)
    {
        string? configPath = null;
        string? outputPath = null;
        bool bigmap = false;
        bool markers = false;
        string? toolsDir = null;
        bool verbose = false;
        bool useMorrowind = false;
        bool useOpenMW = false;
        var layers = new List<LayerInfo>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-c":
                case "--config":
                    if (i + 1 < args.Length) configPath = args[++i];
                    break;
                case "-o":
                case "--output":
                    if (i + 1 < args.Length) outputPath = args[++i];
                    break;
                case "-b":
                case "--bigmap":
                    bigmap = true;
                    break;
                case "-m":
                case "--markers":
                    markers = true;
                    break;
                case "-t":
                case "--tools-dir":
                    if (i + 1 < args.Length) toolsDir = args[++i];
                    break;
                case "-v":
                case "--verbose":
                    verbose = true;
                    break;
                case "--morrowind":
                    useMorrowind = true;
                    break;
                case "--openmw":
                    useOpenMW = true;
                    break;
                case "--underlayer":
                    if (i + 2 < args.Length)
                    {
                        layers.Add(new LayerInfo
                        {
                            Name = args[++i],
                            InputPath = args[++i],
                            IsOverlay = false
                        });
                    }
                    break;
                case "--overlayer":
                    if (i + 2 < args.Length)
                    {
                        layers.Add(new LayerInfo
                        {
                            Name = args[++i],
                            InputPath = args[++i],
                            IsOverlay = true
                        });
                    }
                    break;
            }
        }

        // Handle auto-detection flags
        if (string.IsNullOrEmpty(configPath))
        {
            if (useMorrowind)
            {
                configPath = FindMorrowindIniFromRegistry();
            }
            else if (useOpenMW)
            {
                configPath = FindOpenMWConfig();
            }
        }

        if (string.IsNullOrEmpty(configPath))
        {
            Console.Error.WriteLine("Error: No config specified. Use --config, --morrowind, or --openmw");
            return 1;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.Error.WriteLine("Error: --output is required");
            return 1;
        }

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            if (!File.Exists(configPath))
            {
                logger.LogError("Configuration file not found: {Path}", configPath);
                return 1;
            }

            // Resolve layer paths to full paths
            foreach (var layer in layers)
            {
                layer.InputPath = Path.GetFullPath(layer.InputPath);
            }

            var options = new MapGeneratorOptions
            {
                ConfigPath = Path.GetFullPath(configPath),
                OutputDirectory = Path.GetFullPath(outputPath),
                GenerateBigMap = bigmap,
                GenerateMarkers = markers,
                ToolsDirectory = toolsDir != null ? Path.GetFullPath(toolsDir) : null,
                Layers = layers
            };

            if (layers.Count > 0)
            {
                logger.LogInformation("Additional layers: {Count}", layers.Count);
                foreach (var layer in layers)
                {
                    logger.LogInformation("  - {Name} ({Type}): {Path}",
                        layer.Name, layer.IsOverlay ? "overlay" : "underlay", layer.InputPath);
                }
            }

            var service = new MapGeneratorService(loggerFactory);
            await service.GenerateAsync(options);

            logger.LogInformation("Map generation completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Map generation failed: {Message}", ex.Message);
            return 1;
        }
    }

    static int RunValidate(string[] args)
    {
        string? configPath = null;
        bool useMorrowind = false;
        bool useOpenMW = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-c":
                case "--config":
                    if (i + 1 < args.Length) configPath = args[++i];
                    break;
                case "--morrowind":
                    useMorrowind = true;
                    break;
                case "--openmw":
                    useOpenMW = true;
                    break;
            }
        }

        // Handle auto-detection flags
        if (string.IsNullOrEmpty(configPath))
        {
            if (useMorrowind)
            {
                configPath = FindMorrowindIniFromRegistry();
            }
            else if (useOpenMW)
            {
                configPath = FindOpenMWConfig();
            }
        }

        if (string.IsNullOrEmpty(configPath))
        {
            Console.Error.WriteLine("Error: No config specified. Use --config, --morrowind, or --openmw");
            return 1;
        }

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            if (!File.Exists(configPath))
            {
                logger.LogError("Configuration file not found: {Path}", configPath);
                return 1;
            }

            logger.LogInformation("Validating: {Path}", configPath);

            var parser = new ConfigParserFactory();
            var gameConfig = parser.Parse(configPath);

            logger.LogInformation("Configuration type: {Type}", gameConfig.ConfigType);
            logger.LogInformation("Data paths: {Count}", gameConfig.DataPaths.Count);

            foreach (var path in gameConfig.DataPaths)
            {
                var exists = Directory.Exists(path);
                logger.LogInformation("  {Status} {Path}",
                    exists ? "[OK]" : "[MISSING]", path);
            }

            logger.LogInformation("Plugins: {Count}", gameConfig.EnabledPlugins.Count);

            foreach (var plugin in gameConfig.GetPluginsInLoadOrder())
            {
                var exists = File.Exists(plugin.FullPath);
                logger.LogInformation("  {Status} [{Order}] {Name}",
                    exists ? "[OK]" : "[MISSING]", plugin.LoadOrder, plugin.FileName);
            }

            logger.LogInformation("Archives: {Count}", gameConfig.FallbackArchives.Count);
            foreach (var archive in gameConfig.FallbackArchives)
            {
                logger.LogInformation("  {Archive}", archive);
            }

            var errors = gameConfig.Validate();
            if (errors.Count > 0)
            {
                logger.LogWarning("Validation errors:");
                foreach (var error in errors)
                {
                    logger.LogWarning("  - {Error}", error);
                }
                return 1;
            }

            logger.LogInformation("Configuration is valid!");
            return 0;
        }
        catch (ConfigParseException ex)
        {
            logger.LogError("Parse error: {Message}", ex.Message);
            if (ex.LineNumber.HasValue)
            {
                logger.LogError("  Line: {Line}", ex.LineNumber);
            }
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validation failed: {Message}", ex.Message);
            return 1;
        }
    }

    static async Task<int> RunConvertWebpAsync(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;
        bool lossless = true;
        int quality = 90;
        bool verbose = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-i":
                case "--input":
                    if (i + 1 < args.Length) inputPath = args[++i];
                    break;
                case "-o":
                case "--output":
                    if (i + 1 < args.Length) outputPath = args[++i];
                    break;
                case "--lossy":
                    lossless = false;
                    break;
                case "-q":
                case "--quality":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var q))
                    {
                        quality = Math.Clamp(q, 0, 100);
                    }
                    break;
                case "-v":
                case "--verbose":
                    verbose = true;
                    break;
            }
        }

        if (string.IsNullOrEmpty(inputPath))
        {
            Console.Error.WriteLine("Error: --input is required");
            return 1;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.Error.WriteLine("Error: --output is required");
            return 1;
        }

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            var converter = new PngToWebpConverter(loggerFactory.CreateLogger<PngToWebpConverter>());
            var converted = await converter.ConvertDirectoryAsync(
                Path.GetFullPath(inputPath),
                Path.GetFullPath(outputPath),
                lossless,
                quality);

            logger.LogInformation("Successfully converted {Count} files", converted);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Conversion failed: {Message}", ex.Message);
            return 1;
        }
    }
}
