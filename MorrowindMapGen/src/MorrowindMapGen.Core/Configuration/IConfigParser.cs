namespace MorrowindMapGen.Core.Configuration;

/// <summary>
/// Interface for parsing game configuration files.
/// </summary>
public interface IConfigParser
{
    /// <summary>
    /// Gets the configuration type this parser handles.
    /// </summary>
    ConfigurationType ConfigType { get; }

    /// <summary>
    /// Determines if this parser can handle the specified file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>True if this parser can handle the file.</returns>
    bool CanParse(string filePath);

    /// <summary>
    /// Parses the configuration file and returns a GameConfiguration.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>The parsed game configuration.</returns>
    /// <exception cref="ConfigParseException">Thrown when parsing fails.</exception>
    GameConfiguration Parse(string filePath);
}

/// <summary>
/// Exception thrown when configuration parsing fails.
/// </summary>
public class ConfigParseException : Exception
{
    public string? FilePath { get; }
    public int? LineNumber { get; }

    public ConfigParseException(string message) : base(message) { }

    public ConfigParseException(string message, string filePath) : base(message)
    {
        FilePath = filePath;
    }

    public ConfigParseException(string message, string filePath, int lineNumber) : base(message)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    public ConfigParseException(string message, Exception innerException) : base(message, innerException) { }
}
