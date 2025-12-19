using Avalonia.Labs.Preferences;
using BereitschaftsPlaner.Avalonia.Models;
using System.Text.Json;

namespace BereitschaftsPlaner.Avalonia.Services.Data;

/// <summary>
/// Service for managing application settings using Avalonia.Labs.Preferences
/// Settings are stored platform-specifically:
/// - Windows: Registry
/// - macOS: NSUserDefaults
/// - Linux: XDG Config
/// </summary>
public class SettingsService
{
    private const string SettingsKey = "app_settings";
    private readonly IPreferences _preferences;

    public SettingsService()
    {
        _preferences = Preferences.Default;
    }

    /// <summary>
    /// Load application settings from preferences
    /// Returns default settings if none exist
    /// </summary>
    public AppSettings LoadSettings()
    {
        try
        {
            var json = _preferences.Get<string>(SettingsKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                return GetDefaultSettings();
            }

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
    /// Save application settings to preferences
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        _preferences.Set(SettingsKey, json);
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
        _preferences.Remove(SettingsKey);
    }

    /// <summary>
    /// Check if settings exist
    /// </summary>
    public bool SettingsExist()
    {
        return _preferences.ContainsKey(SettingsKey);
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
