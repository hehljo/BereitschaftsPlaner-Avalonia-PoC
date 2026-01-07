using System;
using System.IO;
using System.Text.Json;
using BereitschaftsPlaner.Avalonia.Models;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for managing Zeitprofile settings (JSON file in config/)
/// </summary>
public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings? _cachedSettings;

    public SettingsService()
    {
        var configDir = FindConfigDirectory();
        _settingsFilePath = Path.Combine(configDir, "settings.json");
        Directory.CreateDirectory(configDir);

        if (!File.Exists(_settingsFilePath))
        {
            var defaultSettings = CreateDefaultSettings();
            SaveSettings(defaultSettings);
        }
    }

    public AppSettings LoadSettings()
    {
        if (_cachedSettings != null) return _cachedSettings;

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
            Console.WriteLine($"Fehler beim Laden: {ex.Message}");
            _cachedSettings = CreateDefaultSettings();
            return _cachedSettings;
        }
    }

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
            throw new Exception($"Fehler beim Speichern: {ex.Message}", ex);
        }
    }

    public void ClearCache() => _cachedSettings = null;
    public string GetSettingsFilePath() => _settingsFilePath;

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

    private string FindConfigDirectory()
    {
        var possiblePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "config"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BereitschaftsPlaner", "config")
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path)) return Path.GetFullPath(path);
        }

        return Path.GetFullPath(possiblePaths[0]);
    }
}

public class AppSettings
{
    public ZeitprofileSettings Zeitprofile { get; set; } = new();
    public string LastExportPath { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production";
}

public class ZeitprofileSettings
{
    public System.Collections.Generic.Dictionary<string, Zeitprofil> Profile { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, string> GruppenZuweisungen { get; set; } = new();
}
