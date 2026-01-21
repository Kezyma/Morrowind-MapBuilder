using System.Text.RegularExpressions;

namespace MorrowindMapGen.Core.Configuration;

/// <summary>
/// Parser for OpenMW openmw.cfg configuration files.
/// </summary>
public partial class OpenMWConfigParser : IConfigParser
{
    public ConfigurationType ConfigType => ConfigurationType.OpenMWCfg;

    public bool CanParse(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var fileName = Path.GetFileName(filePath);
        return fileName.Equals("openmw.cfg", StringComparison.OrdinalIgnoreCase);
    }

    public GameConfiguration Parse(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new ConfigParseException($"Configuration file not found: {filePath}", filePath);
        }

        var config = new GameConfiguration
        {
            SourceConfigPath = Path.GetFullPath(filePath),
            ConfigType = ConfigurationType.OpenMWCfg
        };

        var configDir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? ".";
        var lines = File.ReadAllLines(filePath);
        var dataPaths = new List<string>();
        var contentFiles = new List<string>();
        var fallbackArchives = new List<string>();

        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var line = lines[lineNum].Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            // Parse key=value pairs
            var match = KeyValueRegex().Match(line);
            if (!match.Success)
                continue;

            var key = match.Groups[1].Value.Trim().ToLowerInvariant();
            var value = match.Groups[2].Value.Trim();

            // Remove quotes if present
            value = RemoveQuotes(value);

            switch (key)
            {
                case "data":
                    var dataPath = ResolvePath(value, configDir);
                    if (!string.IsNullOrEmpty(dataPath))
                    {
                        dataPaths.Add(dataPath);
                    }
                    break;

                case "content":
                    contentFiles.Add(value);
                    break;

                case "fallback-archive":
                    fallbackArchives.Add(value);
                    break;
            }
        }

        config.DataPaths.AddRange(dataPaths);
        config.FallbackArchives.AddRange(fallbackArchives);

        // Resolve plugin paths - search through data paths in reverse order (last has priority)
        int loadOrder = 0;
        foreach (var contentFile in contentFiles)
        {
            var pluginPath = FindPluginInDataPaths(contentFile, dataPaths);

            config.EnabledPlugins.Add(new PluginEntry
            {
                FileName = contentFile,
                FullPath = pluginPath ?? Path.Combine(dataPaths.FirstOrDefault() ?? ".", contentFile),
                LoadOrder = loadOrder++
            });
        }

        // Add default archives if none specified
        if (config.FallbackArchives.Count == 0)
        {
            config.FallbackArchives.AddRange(["Morrowind.bsa", "Tribunal.bsa", "Bloodmoon.bsa"]);
        }

        return config;
    }

    /// <summary>
    /// Searches for a plugin file in the data paths, checking in reverse order (last path has priority).
    /// </summary>
    private static string? FindPluginInDataPaths(string pluginFileName, List<string> dataPaths)
    {
        // Search in reverse order - later data paths have priority
        for (int i = dataPaths.Count - 1; i >= 0; i--)
        {
            var candidatePath = Path.Combine(dataPaths[i], pluginFileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        // Not found - return path in first data directory
        return dataPaths.Count > 0 ? Path.Combine(dataPaths[0], pluginFileName) : null;
    }

    /// <summary>
    /// Removes surrounding quotes from a value.
    /// </summary>
    private static string RemoveQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                return value[1..^1];
            }
        }
        return value;
    }

    /// <summary>
    /// Resolves a path, handling relative paths and environment variables.
    /// </summary>
    private static string ResolvePath(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        // Expand environment variables (e.g., %USERPROFILE%)
        path = Environment.ExpandEnvironmentVariables(path);

        // Handle ~ for home directory (Unix-style)
        if (path.StartsWith('~'))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path[1..].TrimStart('/', '\\'));
        }

        // Make absolute if relative
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(basePath, path);
        }

        return Path.GetFullPath(path);
    }

    [GeneratedRegex(@"^([^=]+)=(.*)$")]
    private static partial Regex KeyValueRegex();
}
