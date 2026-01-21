using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.Configuration;

namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Runner for the merge_to_master tool that merges ESP/ESM plugins.
/// </summary>
public class MergeToMasterRunner : ToolRunner
{
    public MergeToMasterRunner(ILogger<MergeToMasterRunner> logger, ToolManager toolManager)
        : base(logger, toolManager)
    {
    }

    protected override ToolInfo Tool => KnownTools.MergeToMaster;

    /// <summary>
    /// Merges all plugins in the configuration into a single master ESM file.
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
        var plugins = config.GetPluginsInLoadOrder().ToList();

        if (plugins.Count == 0)
        {
            throw new ToolException("No plugins to merge.");
        }

        // Validate all plugin files exist
        foreach (var plugin in plugins)
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

        Logger.LogInformation("Merging {Count} plugins into {Output}...", plugins.Count, Path.GetFileName(outputPath));

        // Copy ALL plugins to the output directory with their original names.
        // This is necessary because merge_to_master needs to resolve master dependencies
        // (e.g., Tribunal.esm depends on Morrowind.esm).
        var localPlugins = new List<string>();
        foreach (var plugin in plugins)
        {
            var localPath = Path.Combine(outputDir!, plugin.FileName);
            File.Copy(plugin.FullPath, localPath, overwrite: true);
            localPlugins.Add(localPath);
            Logger.LogDebug("Copied plugin to merge directory: {Plugin}", plugin.FileName);
        }

        // The first plugin becomes our master (already copied with its original name)
        var masterPath = localPlugins[0];

        // Merge each subsequent plugin into the master
        // NOTE: Do NOT delete plugins during this loop - later plugins may depend on earlier ones
        // (e.g., Patch for Purists.esm depends on Tribunal.esm)
        for (int i = 1; i < localPlugins.Count; i++)
        {
            var pluginPath = localPlugins[i];
            Logger.LogDebug("Merging plugin {Index}/{Total}: {Plugin}",
                i + 1, plugins.Count, Path.GetFileName(pluginPath));

            await MergeSinglePluginAsync(pluginPath, masterPath, removeDeleted, cancellationToken);
        }

        // Rename the merged master to the requested output path if different
        if (!string.Equals(masterPath, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            File.Move(masterPath, outputPath, overwrite: true);
        }

        // Clean up all copied plugin files (except the output which was already moved/renamed)
        foreach (var pluginPath in localPlugins)
        {
            if (File.Exists(pluginPath) && !string.Equals(pluginPath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(pluginPath); } catch { /* ignore */ }
            }
        }

        Logger.LogInformation("Successfully merged {Count} plugins", plugins.Count);

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
