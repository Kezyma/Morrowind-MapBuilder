using System.Reflection;
using System.Text;

namespace MorrowindMapGen.Core.Configuration;

/// <summary>
/// Writes OpenMW configuration files for use with the map generator.
/// </summary>
public class OpenMWConfigWriter
{
    private const string OpenMWCfgResourceName = "MorrowindMapGen.Core.Resources.openmw.cfg";
    private const string SettingsCfgResourceName = "MorrowindMapGen.Core.Resources.settings.cfg";

    /// <summary>
    /// Loads the embedded openmw.cfg template.
    /// </summary>
    private static string LoadEmbeddedOpenMWConfig()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(OpenMWCfgResourceName);

        if (stream == null)
        {
            // Return minimal default if resource not found
            return "# OpenMW configuration for map generation\n";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Loads the embedded settings.cfg template.
    /// </summary>
    private static string LoadEmbeddedSettingsConfig()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(SettingsCfgResourceName);

        if (stream == null)
        {
            // Return minimal default if resource not found
            return "# OpenMW settings for map generation\n";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Generates an openmw.cfg file from a GameConfiguration.
    /// </summary>
    /// <param name="config">The game configuration to convert.</param>
    /// <param name="outputPath">Path to write the openmw.cfg file.</param>
    public void WriteOpenMWConfig(GameConfiguration config, string outputPath)
    {
        var sb = new StringBuilder();

        // Start with embedded template
        sb.Append(LoadEmbeddedOpenMWConfig());
        //sb.AppendLine();

        //sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        //sb.AppendLine($"# Source: {config.SourceConfigPath}");
        //sb.AppendLine();

        // Write data paths
        foreach (var dataPath in config.DataPaths)
        {
            // Quote paths that contain spaces
            var quotedPath = $"\"{dataPath}\"";
            sb.AppendLine($"data={quotedPath}");
        }

        // Always include Morrowind.bsa as the first fallback archive
        // This is required for the map generator to work correctly
        sb.AppendLine("fallback-archive=Morrowind.bsa");

        // Write any additional fallback archives from config
        foreach (var archive in config.FallbackArchives)
        {
            // Skip if already added
            if (!archive.Equals("Morrowind.bsa", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"fallback-archive={archive}");
            }
        }

        // Write content (plugins) in load order
        //sb.AppendLine("# Content (load order)");
        foreach (var plugin in config.GetPluginsInLoadOrder())
        {
            sb.AppendLine($"content={plugin.FileName}");
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, sb.ToString());
    }

    /// <summary>
    /// Generates a settings.cfg file with display settings suitable for map generation.
    /// </summary>
    /// <param name="outputPath">Path to write the settings.cfg file.</param>
    public void WriteSettingsConfig(string outputPath)
    {
        var sb = new StringBuilder();

        // Start with embedded template
        sb.Append(LoadEmbeddedSettingsConfig());

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, sb.ToString());
    }

    /// <summary>
    /// Writes both openmw.cfg and settings.cfg to the specified directory.
    /// </summary>
    /// <param name="config">The game configuration to convert.</param>
    /// <param name="outputDirectory">Directory to write configuration files.</param>
    /// <returns>Paths to the generated files (openmw.cfg, settings.cfg).</returns>
    public (string OpenMWConfigPath, string SettingsConfigPath) WriteConfigurations(
        GameConfiguration config,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var openmwConfigPath = Path.Combine(outputDirectory, "openmw.cfg");
        var settingsConfigPath = Path.Combine(outputDirectory, "settings.cfg");

        WriteOpenMWConfig(config, openmwConfigPath);
        WriteSettingsConfig(settingsConfigPath);

        return (openmwConfigPath, settingsConfigPath);
    }
}
