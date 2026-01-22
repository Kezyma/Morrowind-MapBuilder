using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.Configuration;

namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Runner for the merge_to_master tool that merges ESP/ESM plugins.
/// </summary>
public class MergeToMasterRunner : ToolRunner
{
    private readonly ILoggerFactory _loggerFactory;

    public MergeToMasterRunner(ILogger<MergeToMasterRunner> logger, ToolManager toolManager, ILoggerFactory? loggerFactory = null)
        : base(logger, toolManager)
    {
        _loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
    }

    protected override ToolInfo Tool => KnownTools.MergeToMaster;

    /// <summary>
    /// Merges all plugins in the configuration into a single master ESM file.
    /// Uses a smart merge strategy that analyzes plugin dependencies and merges
    /// each plugin into its last listed master to preserve cells and doors.
    /// </summary>
    /// <param name="config">Game configuration with plugins to merge.</param>
    /// <param name="outputPath">Path for the merged ESM output.</param>
    /// <param name="removeDeleted">Whether to remove objects marked as deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the merged ESM file.</returns>
    public async Task<string> MergePluginsAsync(
        GameConfiguration config,
        string outputPath,
        bool removeDeleted = true,
        CancellationToken cancellationToken = default)
    {
        // Get plugins, filtering out unsupported file types
        var configPlugins = config.GetPluginsInLoadOrder()
            .Where(p => !p.FileName.EndsWith(".omwscripts", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (configPlugins.Count == 0)
        {
            throw new ToolException("No plugins to merge.");
        }

        // Validate all plugin files exist
        foreach (var plugin in configPlugins)
        {
            if (!File.Exists(plugin.FullPath))
            {
                throw new ToolException($"Plugin file not found: {plugin.FullPath}");
            }
        }

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        Logger.LogInformation("Merging {Count} plugins into {Output}...", configPlugins.Count, Path.GetFileName(outputPath));

        // Step 1: Copy all plugins to working directory
        Logger.LogInformation("Step 1: Copying plugins to working directory...");
        var pluginInfos = new List<PluginInfo>();

        foreach (var plugin in configPlugins)
        {
            var localPath = Path.Combine(outputDir!, plugin.FileName);
            File.Copy(plugin.FullPath, localPath, overwrite: true);

            pluginInfos.Add(new PluginInfo
            {
                FileName = plugin.FileName,
                FullPath = localPath
            });

            Logger.LogDebug("Copied: {Plugin}", plugin.FileName);
        }

        // Step 2: Convert each plugin to JSON and read dependencies
        Logger.LogInformation("Step 2: Analyzing plugin dependencies...");
        var tes3convRunner = new Tes3ConvRunner(
            _loggerFactory.CreateLogger<Tes3ConvRunner>(),
            ToolManager);

        var mergeCalculator = new PluginMergeCalculator(
            _loggerFactory.CreateLogger<PluginMergeCalculator>());

        foreach (var plugin in pluginInfos)
        {
            var jsonPath = Path.ChangeExtension(plugin.FullPath, ".json");
            await tes3convRunner.ConvertToJsonAsync(plugin.FullPath, jsonPath, cancellationToken);

            plugin.JsonPath = jsonPath;
            plugin.Masters = mergeCalculator.ReadMasterDependencies(jsonPath);

            if (plugin.Masters.Count > 0)
            {
                Logger.LogDebug("{Plugin} depends on: {Masters}",
                    plugin.FileName, string.Join(", ", plugin.Masters));
            }
            else
            {
                Logger.LogDebug("{Plugin} has no masters (base game file)", plugin.FileName);
            }
        }

        // Step 3: Calculate correct merge order
        Logger.LogInformation("Step 3: Calculating merge order...");
        var mergeOperations = mergeCalculator.CalculateMergeOrder(pluginInfos);

        // Step 4: Execute merges in calculated order
        Logger.LogInformation("Step 4: Merging {Count} plugins...", mergeOperations.Count);

        foreach (var op in mergeOperations)
        {
            var sourcePath = op.Source.FullPath;
            var targetPath = Path.Combine(outputDir!, op.TargetFileName);

            Logger.LogDebug("Merging {Source} into {Target}",
                op.Source.FileName, op.TargetFileName);

            await MergeSinglePluginAsync(sourcePath, targetPath, removeDeleted, cancellationToken);
        }

        // Step 5: Identify the final merged master
        // After all merges, find the base master (first plugin with no masters)
        var baseMaster = pluginInfos.FirstOrDefault(p => p.Masters.Count == 0) ?? pluginInfos[0];
        var finalMasterPath = Path.Combine(outputDir!, baseMaster.FileName);

        // Rename to requested output path if different
        if (!string.Equals(finalMasterPath, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            File.Move(finalMasterPath, outputPath, overwrite: true);
        }

        // Clean up: delete all copied plugins and JSON files except output
        Logger.LogDebug("Cleaning up temporary files...");
        foreach (var plugin in pluginInfos)
        {
            // Delete plugin file if not the output
            if (File.Exists(plugin.FullPath) &&
                !string.Equals(plugin.FullPath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(plugin.FullPath); } catch { /* ignore */ }
            }

            // Delete JSON file
            if (!string.IsNullOrEmpty(plugin.JsonPath) && File.Exists(plugin.JsonPath))
            {
                try { File.Delete(plugin.JsonPath); } catch { /* ignore */ }
            }
        }

        Logger.LogInformation("Successfully merged {Count} plugins", configPlugins.Count);

        return outputPath;
    }

    /// <summary>
    /// Merges a single plugin into a master file.
    /// </summary>
    /// <param name="pluginPath">Path to the plugin to merge.</param>
    /// <param name="masterPath">Path to the master file (will be modified).</param>
    /// <param name="removeDeleted">Whether to remove objects marked as deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task MergeSinglePluginAsync(
        string pluginPath,
        string masterPath,
        bool removeDeleted = true,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pluginPath))
        {
            throw new ToolException($"Plugin file not found: {pluginPath}");
        }

        if (!File.Exists(masterPath))
        {
            throw new ToolException($"Master file not found: {masterPath}");
        }

        // Build arguments: merge_to_master.exe [OPTIONS] <PLUGIN> <MASTER>
        var args = new List<string>();

        if (removeDeleted)
        {
            args.Add("-r");
        }

        args.Add("-o"); // Overwrite without backup
        args.Add($"\"{pluginPath}\"");
        args.Add($"\"{masterPath}\"");
        args.Add("--apply-moved-references");
        args.Add("--preserve-duplicate-references");

        var arguments = string.Join(" ", args);
        var result = await RunToolAsync(arguments, cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new ToolException(
                $"merge_to_master failed merging {Path.GetFileName(pluginPath)}: " +
                $"exit code {result.ExitCode}, {result.StandardError}");
        }
    }
}
