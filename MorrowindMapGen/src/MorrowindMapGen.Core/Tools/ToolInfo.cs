namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Information about an external tool required by the map generator.
/// </summary>
public class ToolInfo
{
    /// <summary>
    /// The name of the tool.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The version of the tool.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The download URL for the tool.
    /// </summary>
    public required string DownloadUrl { get; init; }

    /// <summary>
    /// The expected executable name after extraction.
    /// </summary>
    public required string ExecutableName { get; init; }

    /// <summary>
    /// Optional subdirectory within the zip where the executable is located.
    /// </summary>
    public string? ZipSubdirectory { get; init; }
}

/// <summary>
/// Pre-defined tool information for required external tools.
/// </summary>
public static class KnownTools
{
    public static ToolInfo OpenMWMapGen { get; } = new()
    {
        Name = "openmw-map-gen",
        Version = "V2",
        DownloadUrl = "https://github.com/Diject/openmw-map-gen/releases/download/V5/openmw-map-gen-windows.zip",
        ExecutableName = "openmw.exe"
    };

    public static ToolInfo MergeToMaster { get; } = new()
    {
        Name = "merge_to_master",
        Version = "v0.9.15",
        DownloadUrl = "https://github.com/Greatness7/merge_to_master/releases/download/v0.9.15/merge_to_master_v0.9.15_windows.zip",
        ExecutableName = "merge_to_master.exe"
    };

    public static ToolInfo Tes3Conv { get; } = new()
    {
        Name = "tes3conv",
        Version = "v0.4.1",
        DownloadUrl = "https://github.com/Greatness7/tes3conv/releases/download/v0.4.1/windows-latest.zip",
        ExecutableName = "tes3conv.exe"
    };

    public static IReadOnlyList<ToolInfo> All { get; } = [OpenMWMapGen, MergeToMaster, Tes3Conv];
}
