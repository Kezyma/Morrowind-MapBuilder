using System.Diagnostics;
using System.Text.RegularExpressions;
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

            // Run the tool with file system monitoring to detect when interior cells start
            var result = await RunWithInteriorDetectionAsync(
                toolPath,
                arguments,
                localMapOutput,
                toolDir,
                TimeSpan.FromHours(2),
                cancellationToken);

            if (!result.Success && !result.WasTerminatedEarly)
            {
                throw new ToolException($"OpenMW Map Generator failed with exit code {result.ExitCode}: {result.StandardError}");
            }

            if (result.WasTerminatedEarly)
            {
                Logger.LogInformation("Terminated early after detecting interior cell generation");
            }

            // Verify tiles were generated
            var tileFiles = Directory.GetFiles(localMapOutput, "*.png", SearchOption.AllDirectories);
            if (tileFiles.Length == 0)
            {
                throw new ToolException($"No tile files were generated in {localMapOutput}");
            }

            Logger.LogInformation("Map generation complete. Generated {Count} exterior tile files in {Duration:F1}s",
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

    /// <summary>
    /// Result of running the map generator with early termination support.
    /// </summary>
    private class MapGenRunResult
    {
        public int ExitCode { get; init; }
        public string StandardOutput { get; init; } = string.Empty;
        public string StandardError { get; init; } = string.Empty;
        public TimeSpan Duration { get; init; }
        public bool WasTerminatedEarly { get; init; }
        public bool Success => ExitCode == 0 || WasTerminatedEarly;
    }

    /// <summary>
    /// Regex pattern to match coordinate pattern in filenames (e.g., "(0, -11)" or "(-5,3)").
    /// </summary>
    private static readonly Regex CoordinatePattern = new(@"\(-?\d+,\s*-?\d+\)", RegexOptions.Compiled);

    /// <summary>
    /// Runs the map generator with file system monitoring to detect interior cells.
    /// When an interior cell is detected (file without coordinates), the process is terminated early.
    /// </summary>
    private async Task<MapGenRunResult> RunWithInteriorDetectionAsync(
        string toolPath,
        string arguments,
        string outputDirectory,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var wasTerminatedEarly = false;

        var startInfo = new ProcessStartInfo
        {
            FileName = toolPath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                Logger.LogTrace("[openmw-map-gen] {Output}", e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                Logger.LogTrace("[openmw-map-gen] [ERR] {Output}", e.Data);
            }
        };

        // Set up file system watcher to detect interior cells
        using var watcher = new FileSystemWatcher(outputDirectory)
        {
            Filter = "*.png",
            NotifyFilter = NotifyFilters.FileName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = false // Enable after process starts
        };

        var interiorDetectedCts = new CancellationTokenSource();
        var exteriorTileCount = 0;

        watcher.Created += (_, e) =>
        {
            var fileName = Path.GetFileNameWithoutExtension(e.Name);

            // Check if filename contains coordinate pattern
            if (!CoordinatePattern.IsMatch(fileName ?? string.Empty))
            {
                // This is an interior cell - no coordinates in filename
                Logger.LogDebug("Detected interior cell: {FileName}", fileName);
                interiorDetectedCts.Cancel();
            }
            else
            {
                Interlocked.Increment(ref exteriorTileCount);
                if (exteriorTileCount % 100 == 0)
                {
                    Logger.LogDebug("Generated {Count} exterior tiles...", exteriorTileCount);
                }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Enable the watcher now that the process is running
        watcher.EnableRaisingEvents = true;

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token,
            interiorDetectedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (interiorDetectedCts.IsCancellationRequested)
        {
            // Interior cell detected - terminate early
            wasTerminatedEarly = true;
            Logger.LogInformation("Interior cell detected after {Count} exterior tiles - terminating process...", exteriorTileCount);

            try
            {
                process.Kill(entireProcessTree: true);
                // Give it a moment to clean up
                await Task.Delay(500, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Error killing process: {Message}", ex.Message);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new ToolException($"OpenMW Map Generator timed out after {timeout}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw;
        }

        sw.Stop();
        watcher.EnableRaisingEvents = false;

        return new MapGenRunResult
        {
            ExitCode = wasTerminatedEarly ? 0 : (process.HasExited ? process.ExitCode : -1),
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString(),
            Duration = sw.Elapsed,
            WasTerminatedEarly = wasTerminatedEarly
        };
    }
}
