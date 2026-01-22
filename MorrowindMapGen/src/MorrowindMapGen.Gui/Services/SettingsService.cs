using System.Text.Json;
using MorrowindMapGen.Gui.Models;

namespace MorrowindMapGen.Gui.Services;

/// <summary>
/// Service for loading and saving application settings.
/// Settings are stored next to the executable for portability.
/// </summary>
public class SettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SettingsService()
    {
        // Store settings next to the executable
        var exePath = AppContext.BaseDirectory;
        _settingsPath = Path.Combine(exePath, SettingsFileName);
    }

    /// <summary>
    /// Gets the path where settings are stored.
    /// </summary>
    public string SettingsPath => _settingsPath;

    /// <summary>
    /// Loads settings from disk, or returns default settings if none exist.
    /// </summary>
    public GuiSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<GuiSettings>(json, JsonOptions);
                return settings ?? GuiSettings.CreateDefault();
            }
        }
        catch (Exception)
        {
            // If loading fails, return default settings
        }

        return GuiSettings.CreateDefault();
    }

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    public void Save(GuiSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes the settings file if it exists.
    /// </summary>
    public void Reset()
    {
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }
    }
}
