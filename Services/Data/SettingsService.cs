using System;
using System.IO;
using System.Text.Json;
using BereitschaftsPlaner.Avalonia.Models;

namespace BereitschaftsPlaner.Avalonia.Services.Data;

/// <summary>
/// Service for managing application settings using JSON file storage
/// Settings are stored in platform-specific AppData folder
/// </summary>
public class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "BereitschaftsPlaner");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    /// <summary>
    /// Load application settings from JSON file
    /// Returns default settings if none exist
    /// </summary>
    public AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return GetDefaultSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? GetDefaultSettings();
        }
        catch (Exception)
        {
            // If deserialization fails, return defaults
            return GetDefaultSettings();
        }
    }

    /// <summary>
    /// Save application settings to JSON file
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsPath, json);
    }

    /// <summary>
    /// Get default application settings
    /// </summary>
    public AppSettings GetDefaultSettings()
    {
        return new AppSettings
        {
            Bundesland = "BY",
            FeiertagsBehandlung = "Sonntag",
            DarkModeEnabled = true,
            AppVersion = GetAppVersion()
        };
    }

    /// <summary>
    /// Update a specific setting value
    /// </summary>
    public void UpdateSetting<T>(Action<AppSettings> updateAction)
    {
        var settings = LoadSettings();
        updateAction(settings);
        SaveSettings(settings);
    }

    /// <summary>
    /// Clear all settings (reset to defaults)
    /// </summary>
    public void ClearSettings()
    {
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }
    }

    /// <summary>
    /// Check if settings exist
    /// </summary>
    public bool SettingsExist()
    {
        return File.Exists(_settingsPath);
    }

    /// <summary>
    /// Get current application version
    /// </summary>
    private string GetAppVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    /// <summary>
    /// Migrate settings from old version if needed
    /// </summary>
    public void MigrateSettingsIfNeeded()
    {
        var settings = LoadSettings();
        var currentVersion = GetAppVersion();

        if (settings.AppVersion != currentVersion)
        {
            // Perform any necessary migrations here
            settings.AppVersion = currentVersion;
            SaveSettings(settings);
        }
    }
}
