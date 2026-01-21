namespace MorrowindMapGen.Core.Configuration;

/// <summary>
/// Represents an enabled game plugin (ESM/ESP file).
/// </summary>
public class PluginEntry
{
    /// <summary>
    /// The filename of the plugin (e.g., "Morrowind.esm").
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// The full path to the plugin file.
    /// </summary>
    public required string FullPath { get; set; }

    /// <summary>
    /// The load order index (0 = first to load).
    /// </summary>
    public int LoadOrder { get; set; }

    public override string ToString() => $"[{LoadOrder}] {FileName}";
}
