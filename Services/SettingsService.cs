using System;
using System.IO;
using System.Text.Json;
using BereitschaftsPlaner.Avalonia.Models;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for managing application settings (JSON file)
/// Matches PowerShell SettingsManager.ps1 functionality
/// </summary>
public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings? _cachedSettings;

    public SettingsService()
    {
        // Find config directory
        var configDir = FindConfigDirectory();
        _settingsFilePath = Path.Combine(configDir, "settings.json");

        // Ensure config directory exists
        Directory.CreateDirectory(configDir);

        // Initialize settings if file doesn't exist
        if (!File.Exists(_settingsFilePath))
        {
            var defaultSettings = CreateDefaultSettings();
            SaveSettings(defaultSettings);
        }
    }

    /// <summary>
    /// Loads settings from JSON file
    /// </summary>
    public AppSettings LoadSettings()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _cachedSettings = CreateDefaultSettings();
                return _cachedSettings;
            }

            var json = File.ReadAllText(_settingsFilePath);
            _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            }) ?? CreateDefaultSettings();

            return _cachedSettings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Laden der Settings: {ex.Message}");
            _cachedSettings = CreateDefaultSettings();
            return _cachedSettings;
        }
    }

    /// <summary>
    /// Saves settings to JSON file
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });

            File.WriteAllText(_settingsFilePath, json);
            _cachedSettings = settings;
        }
        catch (Exception ex)
        {
            throw new Exception($"Fehler beim Speichern der Settings: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Clears cached settings (force reload on next access)
    /// </summary>
    public void ClearCache()
    {
        _cachedSettings = null;
    }

    /// <summary>
    /// Gets the path to the settings file
    /// </summary>
    public string GetSettingsFilePath()
    {
        return _settingsFilePath;
    }

    /// <summary>
    /// Creates default settings structure
    /// </summary>
    private AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            Zeitprofile = new ZeitprofileSettings
            {
                Profile = new System.Collections.Generic.Dictionary<string, Zeitprofil>(),
                GruppenZuweisungen = new System.Collections.Generic.Dictionary<string, string>()
            },
            LastExportPath = string.Empty,
            Environment = "Production"
        };
    }

    /// <summary>
    /// Finds the config directory in various possible locations
    /// </summary>
    private string FindConfigDirectory()
    {
        var possiblePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "config"),
            Path.Combine(Environment.CurrentDirectory, "config"),
            Path.Combine("/root/BereitschaftsPlaner-Avalonia-PoC", "config"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BereitschaftsPlaner", "config")
        };

        // Try to find existing config directory
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        // Use first path as default (create it)
        return Path.GetFullPath(possiblePaths[0]);
    }
}

/// <summary>
/// Root settings model
/// </summary>
public class AppSettings
{
    public ZeitprofileSettings Zeitprofile { get; set; } = new();
    public string LastExportPath { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production";
}

/// <summary>
/// Zeitprofile settings container
/// </summary>
public class ZeitprofileSettings
{
    public System.Collections.Generic.Dictionary<string, Zeitprofil> Profile { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, string> GruppenZuweisungen { get; set; } = new();
}
