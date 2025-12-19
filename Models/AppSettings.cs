namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Application settings and user preferences
/// </summary>
public class AppSettings
{
    public string LastImportPath { get; set; } = string.Empty;
    public string LastExportPath { get; set; } = string.Empty;
    public string Bundesland { get; set; } = "BY"; // Default: Bayern
    public string FeiertagsBehandlung { get; set; } = "Sonntag"; // Treat holidays like Sundays
    public string LastTemplatePath { get; set; } = string.Empty;
    public string AppVersion { get; set; } = "1.0.0";
    public DateTime LastBackupDate { get; set; } = DateTime.MinValue;

    // UI Preferences
    public bool DarkModeEnabled { get; set; } = true;
    public string LastSelectedGruppe { get; set; } = string.Empty;
    public string LastSelectedZeitprofil { get; set; } = string.Empty;
}
