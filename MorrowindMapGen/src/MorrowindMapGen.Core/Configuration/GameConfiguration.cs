namespace MorrowindMapGen.Core.Configuration;

/// <summary>
/// Represents the parsed game configuration containing data paths and enabled plugins.
/// </summary>
public class GameConfiguration
{
    /// <summary>
    /// The path to the original configuration file that was parsed.
    /// </summary>
    public required string SourceConfigPath { get; set; }

    /// <summary>
    /// The type of configuration file (Morrowind.ini or openmw.cfg).
    /// </summary>
    public ConfigurationType ConfigType { get; set; }

    /// <summary>
    /// List of data directories containing game assets.
    /// For Morrowind.ini, this is typically just the "Data Files" folder.
    /// For OpenMW, this can include multiple data directories.
    /// </summary>
    public List<string> DataPaths { get; set; } = [];

    /// <summary>
    /// List of BSA archive files to use as fallbacks.
    /// </summary>
    public List<string> FallbackArchives { get; set; } = [];

    /// <summary>
    /// List of enabled plugins in load order.
    /// </summary>
    public List<PluginEntry> EnabledPlugins { get; set; } = [];

    /// <summary>
    /// Validates that the configuration is complete and usable.
    /// </summary>
    /// <returns>A list of validation errors, empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (DataPaths.Count == 0)
        {
            errors.Add("No data paths configured.");
        }

        foreach (var dataPath in DataPaths)
        {
            if (!Directory.Exists(dataPath))
            {
                errors.Add($"Data path does not exist: {dataPath}");
            }
        }

        if (EnabledPlugins.Count == 0)
        {
            errors.Add("No plugins enabled.");
        }

        foreach (var plugin in EnabledPlugins)
        {
            if (!File.Exists(plugin.FullPath))
            {
                errors.Add($"Plugin file not found: {plugin.FullPath}");
            }
        }

        return errors;
    }

    /// <summary>
    /// Gets the plugin files sorted by load order.
    /// </summary>
    public IEnumerable<PluginEntry> GetPluginsInLoadOrder()
    {
        return EnabledPlugins.OrderBy(p => p.LoadOrder);
    }
}
