using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.Configuration;

namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Options for the OpenMW Map Generator.
/// </summary>
public class MapGenOptions
{
    /// <summary>
    /// Output directory for local map tiles (256x256 per cell).
    /// </summary>
    public string? LocalMapOutput { get; set; }

    /// <summary>
    /// Output directory for world map texture.
    /// </summary>
    public string? WorldMapOutput { get; set; }

    /// <summary>
    /// Whether to overwrite existing map files.
    /// </summary>
    public bool OverwriteMaps { get; set; } = true;

    /// <summary>
    /// Downscale factor for world map generation from tiles.
    /// Set to -1 to disable, 1 for full size, 2 for half size, etc.
    /// </summary>
    public int TilemapDownscaleFactor { get; set; } = -1;

    /// <summary>
    /// Pixels per cell for world map (default: 32).
    /// </summary>
    public int WorldMapPixelsPerCell { get; set; } = 32;

    /// <summary>
    /// Border pixels around terrain above water (default: 2).
    /// </summary>
    public int WorldMapBorder { get; set; } = 2;
}

/// <summary>
/// Runner for the OpenMW Map Generator (openmw-map-gen fork).
/// </summary>
public class OpenMWMapGenRunner : ToolRunner
{
    private readonly OpenMWConfigWriter _configWriter;

    public OpenMWMapGenRunner(ILogger<OpenMWMapGenRunner> logger, ToolManager toolManager)
        : base(logger, toolManager)
    {
        _configWriter = new OpenMWConfigWriter();
    }

    protected override ToolInfo Tool => KnownTools.OpenMWMapGen;

    /// <summary>
    /// Generates map tiles using the OpenMW Map Generator.
    /// </summary>
    /// <param name="config">Game configuration with data paths and plugins.</param>
    /// <param name="outputDirectory">Directory to output generated tiles.</param>
    /// <param name="options">Map generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the directory containing generated tiles.</returns>
    public async Task<string> GenerateMapAsync(
        GameConfiguration config,
        string outputDirectory,
        MapGenOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MapGenOptions();

        // Get the tool executable path
        var toolPath = await ToolManager.GetToolPathAsync(Tool, cancellationToken);
        var toolDir = Path.GetDirectoryName(toolPath)!;

        // Create a temp directory for our generated configs (NOT in tool directory)
        var configTempDir = Path.GetFullPath($"mwmapgen-config");
        Directory.CreateDirectory(configTempDir);

        try
        {
            // Create output directories
            var localMapOutput = options.LocalMapOutput ?? Path.Combine(outputDirectory, "tiles");
            Directory.CreateDirectory(localMapOutput);

            // Write OpenMW configuration files to temp directory
            Logger.LogInformation("Generating OpenMW configuration in temp folder...");
            var (openmwConfigPath, settingsConfigPath) = _configWriter.WriteConfigurations(config, configTempDir);

            Logger.LogDebug("Config written to: {Path}", openmwConfigPath);
            Logger.LogDebug("Settings written to: {Path}", settingsConfigPath);

            // Build command line arguments
            // Pass config file path using --config argument (same as running openmw to play)
            var args = new List<string>
            {
                "--replace",
                "config",
                "--config",
                $"\"{configTempDir}\"",
            };

            args.Add($"--local-map-output=\"{localMapOutput}\"");

            if (!string.IsNullOrEmpty(options.WorldMapOutput))
            {
                args.Add($"--world-map-output=\"{options.WorldMapOutput}\"");
            }

            if (options.OverwriteMaps)
            {
                args.Add("--overwrite-maps");
            }

            if (options.TilemapDownscaleFactor > 0)
            {
                args.Add($"--tilemap-downscale-factor={options.TilemapDownscaleFactor}");
            }

            if (options.WorldMapPixelsPerCell != 32)
            {
                args.Add($"--world-map-pixelsPerCell={options.WorldMapPixelsPerCell}");
            }

            if (options.WorldMapBorder != 2)
            {
                args.Add($"--world-map-border={options.WorldMapBorder}");
            }

            var arguments = string.Join(" ", args);

            Logger.LogInformation("Starting OpenMW Map Generator...");
            Logger.LogWarning("Note: Do NOT minimize the OpenMW window - this will pause map extraction!");

            // Run from the tool directory but with our custom config
            var result = await RunToolAsync(
                arguments,
                workingDirectory: toolDir,
                timeout: TimeSpan.FromHours(2),
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                throw new ToolException($"OpenMW Map Generator failed with exit code {result.ExitCode}: {result.StandardError}");
            }

            // Check for completion message in output
            if (!result.StandardOutput.Contains("Map extraction complete", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("Map extraction may not have completed successfully - completion message not found in output");
            }

            // Verify tiles were generated
            var tileFiles = Directory.GetFiles(localMapOutput, "*.png", SearchOption.AllDirectories);
            if (tileFiles.Length == 0)
            {
                throw new ToolException($"No tile files were generated in {localMapOutput}");
            }

            Logger.LogInformation("Map generation complete. Generated {Count} tile files in {Duration:F1}s",
                tileFiles.Length, result.Duration.TotalSeconds);

            return localMapOutput;
        }
        finally
        {
            // Clean up temp config directory
            try
            {
                if (Directory.Exists(configTempDir))
                {
                    Directory.Delete(configTempDir, recursive: true);
                    Logger.LogDebug("Cleaned up temp config directory");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to clean up temp config directory: {Message}", ex.Message);
            }
        }
    }

    /// <summary>
    /// Checks if the OpenMW Map Generator tool is available.
    /// </summary>
    public bool IsAvailable()
    {
        return ToolManager.IsToolAvailable(Tool);
    }
}
