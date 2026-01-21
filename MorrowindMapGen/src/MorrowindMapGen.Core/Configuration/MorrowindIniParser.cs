using System.Text.RegularExpressions;

namespace MorrowindMapGen.Core.Configuration;

/// <summary>
/// Parser for classic Morrowind.ini configuration files.
/// </summary>
public partial class MorrowindIniParser : IConfigParser
{
    public ConfigurationType ConfigType => ConfigurationType.MorrowindIni;

    public bool CanParse(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var fileName = Path.GetFileName(filePath);
        return fileName.Equals("Morrowind.ini", StringComparison.OrdinalIgnoreCase);
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
            ConfigType = ConfigurationType.MorrowindIni
        };

        // Data Files is in the same directory as Morrowind.ini
        var morrowindDir = Path.GetDirectoryName(Path.GetFullPath(filePath))
            ?? throw new ConfigParseException("Could not determine Morrowind directory.", filePath);

        var dataFilesPath = Path.Combine(morrowindDir, "Data Files");
        if (Directory.Exists(dataFilesPath))
        {
            config.DataPaths.Add(dataFilesPath);
        }
        else
        {
            throw new ConfigParseException($"Data Files directory not found: {dataFilesPath}", filePath);
        }

        // Parse the INI file
        var lines = File.ReadAllLines(filePath);
        var currentSection = string.Empty;
        var gameFiles = new Dictionary<int, string>();
        var archives = new Dictionary<int, string>();

        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var line = lines[lineNum].Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith(';'))
                continue;

            // Check for section header
            var sectionMatch = SectionRegex().Match(line);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups[1].Value;
                continue;
            }

            // Parse key=value pairs
            var keyValueMatch = KeyValueRegex().Match(line);
            if (!keyValueMatch.Success)
                continue;

            var key = keyValueMatch.Groups[1].Value;
            var value = keyValueMatch.Groups[2].Value;

            // Handle Game Files section
            if (currentSection.Equals("Game Files", StringComparison.OrdinalIgnoreCase))
            {
                var gameFileMatch = GameFileKeyRegex().Match(key);
                if (gameFileMatch.Success)
                {
                    var index = int.Parse(gameFileMatch.Groups[1].Value);
                    gameFiles[index] = value;
                }
            }
            // Handle Archives section
            else if (currentSection.Equals("Archives", StringComparison.OrdinalIgnoreCase))
            {
                var archiveMatch = ArchiveKeyRegex().Match(key);
                if (archiveMatch.Success)
                {
                    var index = int.Parse(archiveMatch.Groups[1].Value);
                    archives[index] = value;
                }
            }
        }

        // Build plugin list in load order
        foreach (var kvp in gameFiles.OrderBy(x => x.Key))
        {
            var pluginFileName = kvp.Value;
            var pluginPath = Path.Combine(dataFilesPath, pluginFileName);

            config.EnabledPlugins.Add(new PluginEntry
            {
                FileName = pluginFileName,
                FullPath = pluginPath,
                LoadOrder = kvp.Key
            });
        }

        // Build archives list
        foreach (var kvp in archives.OrderBy(x => x.Key))
        {
            config.FallbackArchives.Add(kvp.Value);
        }

        // Add default archives if none specified
        if (config.FallbackArchives.Count == 0)
        {
            config.FallbackArchives.AddRange(["Morrowind.bsa", "Tribunal.bsa", "Bloodmoon.bsa"]);
        }

        return config;
    }

    [GeneratedRegex(@"^\[([^\]]+)\]$")]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"^([^=]+)=(.*)$")]
    private static partial Regex KeyValueRegex();

    [GeneratedRegex(@"^GameFile(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex GameFileKeyRegex();

    [GeneratedRegex(@"^Archive\s*(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ArchiveKeyRegex();
}
