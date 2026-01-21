namespace MorrowindMapGen.Core.Configuration;

/// <summary>
/// Factory for creating the appropriate configuration parser based on file type.
/// </summary>
public class ConfigParserFactory
{
    private readonly IReadOnlyList<IConfigParser> _parsers;

    public ConfigParserFactory()
    {
        _parsers = new List<IConfigParser>
        {
            new MorrowindIniParser(),
            new OpenMWConfigParser()
        };
    }

    /// <summary>
    /// Gets the appropriate parser for the given configuration file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>The parser that can handle the file.</returns>
    /// <exception cref="ConfigParseException">Thrown when no parser can handle the file.</exception>
    public IConfigParser GetParser(string filePath)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanParse(filePath));

        if (parser == null)
        {
            var fileName = Path.GetFileName(filePath);
            throw new ConfigParseException(
                $"Unknown configuration file type: {fileName}. " +
                "Expected 'Morrowind.ini' or 'openmw.cfg'.",
                filePath);
        }

        return parser;
    }

    /// <summary>
    /// Parses a configuration file, automatically detecting the format.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>The parsed game configuration.</returns>
    public GameConfiguration Parse(string filePath)
    {
        var parser = GetParser(filePath);
        return parser.Parse(filePath);
    }

    /// <summary>
    /// Detects the configuration type from the file path.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>The detected configuration type, or null if unknown.</returns>
    public ConfigurationType? DetectType(string filePath)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanParse(filePath));
        return parser?.ConfigType;
    }
}
