namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Zentrale Debug-Konfiguration für Logging
/// Ermöglicht das Ein-/Ausschalten von Debug-Logs nach Menüpunkt
///
/// VERWENDUNG:
/// if (DebugConfig.Import) Serilog.Log.Debug("...");
///
/// AKTIVIEREN/DEAKTIVIEREN:
/// Einfach den bool-Wert auf true/false setzen
/// </summary>
public static class DebugConfig
{
    // ============================================================================
    // MENÜPUNKT: IMPORT
    // ============================================================================

    /// <summary>
    /// Debug-Logs für Excel-Import (Ressourcen, Gruppen)
    /// </summary>
    public static bool Import = false;  // ← Hier ändern um zu aktivieren/deaktivieren

    /// <summary>
    /// Debug-Logs für Import Preview Dialog
    /// </summary>
    public static bool ImportPreview = false;

    /// <summary>
    /// Debug-Logs für DataGrid (Anzeige von Ressourcen/Gruppen)
    /// </summary>
    public static bool DataGrid = false;

    // ============================================================================
    // MENÜPUNKT: ZEITPROFILE
    // ============================================================================

    /// <summary>
    /// Debug-Logs für Zeitprofile-Verwaltung
    /// </summary>
    public static bool Zeitprofile = true;  // ← Aktuell aktiv (für Entwicklung)

    /// <summary>
    /// Debug-Logs für Feiertags-Berechnung
    /// </summary>
    public static bool Feiertage = true;

    // ============================================================================
    // MENÜPUNKT: PLANUNG (ERSTELLEN)
    // ============================================================================

    /// <summary>
    /// Debug-Logs für Planning Board (Kalender-Ansicht)
    /// </summary>
    public static bool Planning = false;

    /// <summary>
    /// Debug-Logs für Auto-Fill Algorithmus
    /// </summary>
    public static bool AutoFill = false;

    /// <summary>
    /// Debug-Logs für Fairness-Berechnung
    /// </summary>
    public static bool Fairness = false;

    // ============================================================================
    // MENÜPUNKT: EDITOR
    // ============================================================================

    /// <summary>
    /// Debug-Logs für Bereitschafts-Editor
    /// </summary>
    public static bool Editor = false;

    // ============================================================================
    // SONSTIGE
    // ============================================================================

    /// <summary>
    /// Debug-Logs für Backup/Restore
    /// </summary>
    public static bool Backup = false;

    /// <summary>
    /// Debug-Logs für Settings (AppSettings, SettingsService)
    /// </summary>
    public static bool Settings = false;

    /// <summary>
    /// Debug-Logs für Datenbank-Operationen (LiteDB)
    /// </summary>
    public static bool Database = false;

    /// <summary>
    /// Debug-Logs für MainWindowViewModel
    /// </summary>
    public static bool MainWindow = false;

    /// <summary>
    /// Debug-Logs für Template-System
    /// </summary>
    public static bool Templates = false;

    /// <summary>
    /// Debug-Logs für History/Analysis
    /// </summary>
    public static bool History = false;

    // ============================================================================
    // MASTER SWITCH
    // ============================================================================

    /// <summary>
    /// Master Switch - Deaktiviert ALLE Debug-Logs wenn false
    /// Nützlich für Production Builds
    /// </summary>
    public static bool Enabled = true;

    /// <summary>
    /// Helper: Prüft ob Debug-Logging für einen Bereich aktiv ist
    /// </summary>
    public static bool IsEnabled(bool category) => Enabled && category;
}
