using Microsoft.Win32;

namespace MorrowindMapGen.Gui.Services;

/// <summary>
/// Service for auto-detecting game configuration file paths.
/// </summary>
public class ConfigDetectionService
{
    /// <summary>
    /// Gets the default OpenMW configuration path.
    /// </summary>
    public string GetDefaultOpenMWConfigPath()
    {
        // OpenMW stores config in %APPDATA%/openmw/openmw.cfg on Windows
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "openmw", "openmw.cfg");
    }

    /// <summary>
    /// Gets the default Morrowind.ini path from common locations.
    /// </summary>
    public string? GetDefaultMorrowindIniPath()
    {
        // Try registry first
        var registryPath = TryGetMorrowindPathFromRegistry();
        if (!string.IsNullOrEmpty(registryPath))
        {
            var iniPath = Path.Combine(registryPath, "Morrowind.ini");
            if (File.Exists(iniPath))
            {
                return iniPath;
            }
        }

        // Try common installation paths
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Morrowind",
            @"C:\Program Files (x86)\Bethesda Softworks\Morrowind",
            @"C:\Program Files (x86)\GOG Galaxy\Games\Morrowind",
            @"C:\GOG Games\Morrowind",
            @"D:\Steam\steamapps\common\Morrowind",
            @"D:\Games\Morrowind"
        };

        foreach (var basePath in commonPaths)
        {
            var iniPath = Path.Combine(basePath, "Morrowind.ini");
            if (File.Exists(iniPath))
            {
                return iniPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to detect the OpenMW configuration path.
    /// </summary>
    public bool TryDetectOpenMW(out string path)
    {
        path = GetDefaultOpenMWConfigPath();
        return File.Exists(path);
    }

    /// <summary>
    /// Tries to detect the Morrowind.ini path.
    /// </summary>
    public bool TryDetectMorrowind(out string? path)
    {
        path = GetDefaultMorrowindIniPath();
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    /// <summary>
    /// Validates that a configuration path exists and is valid.
    /// </summary>
    public bool ValidateConfigPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!File.Exists(path))
            return false;

        var fileName = Path.GetFileName(path).ToLowerInvariant();
        return fileName is "openmw.cfg" or "morrowind.ini";
    }

    /// <summary>
    /// Gets the game type from a configuration file path.
    /// </summary>
    public Models.GameType? GetGameTypeFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var fileName = Path.GetFileName(path).ToLowerInvariant();
        return fileName switch
        {
            "openmw.cfg" => Models.GameType.OpenMW,
            "morrowind.ini" => Models.GameType.Morrowind,
            _ => null
        };
    }

    /// <summary>
    /// Tries to get the Morrowind installation path from the Windows registry.
    /// </summary>
    private static string? TryGetMorrowindPathFromRegistry()
    {
        try
        {
            // Try Bethesda registry key
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Bethesda Softworks\Morrowind");
            if (key != null)
            {
                var path = key.GetValue("Installed Path") as string;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    return path;
                }
            }

            // Try Steam registry key
            using var steamKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 22320");
            if (steamKey != null)
            {
                var path = steamKey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    return path;
                }
            }
        }
        catch
        {
            // Registry access can fail, ignore
        }

        return null;
    }
}
