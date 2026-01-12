using System;
using System.Collections.Generic;

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
    public bool DarkModeEnabled { get; set; } = false; // Default: Light Mode
    public string Environment { get; set; } = "Production"; // Production or QA
    public string LastSelectedGruppe { get; set; } = string.Empty;
    public string LastSelectedZeitprofil { get; set; } = string.Empty;

    // Zeitprofile Configuration
    public ZeitprofileSettings Zeitprofile { get; set; } = new();

    // Excel Import Configuration
    public ExcelImportSettings ExcelImport { get; set; } = new();

    // Feature Flags (enable/disable advanced features)
    public FeatureFlags Features { get; set; } = new();
}

/// <summary>
/// Zeitprofile configuration container
/// </summary>
public class ZeitprofileSettings
{
    // Dictionary of all available profiles (Key = ProfilID, Value = Zeitprofil)
    public Dictionary<string, Zeitprofil> Profile { get; set; } = new();

    // Group to profile assignments (Key = GruppenName, Value = ProfilID)
    public Dictionary<string, string> GruppenZuweisungen { get; set; } = new();
}

/// <summary>
/// Feature flags for optional advanced features
/// </summary>
public class FeatureFlags
{
    // Tier 1: Core Features
    public bool AutoFillEnabled { get; set; } = true;
    public bool FairnessDashboardEnabled { get; set; } = true;
    public bool VacationCalendarEnabled { get; set; } = true;

    // Tier 2: Quality-of-Life
    public bool HistoricalAnalysisEnabled { get; set; } = true;
    public bool ShiftSwapEnabled { get; set; } = false; // Requires approval workflow
    public bool ConflictAssistantEnabled { get; set; } = true;
    public bool TemplatLibraryEnabled { get; set; } = true;

    // Tier 3: Professional
    public bool WorkloadHeatmapEnabled { get; set; } = true;
    public bool SkillsMatchingEnabled { get; set; } = false; // Requires skills database
    public bool MultiTeamCoordinationEnabled { get; set; } = false; // Multi-org feature

    // Tier 4: Integration
    public bool NotificationSystemEnabled { get; set; } = false; // Requires email config
    public bool CalendarIntegrationEnabled { get; set; } = true; // ICS export
    public bool MobileViewEnabled { get; set; } = false; // Future feature

    // Tier 5: Advanced Intelligence
    public bool FairnessRulesEnabled { get; set; } = true;
    public bool WhatIfScenariosEnabled { get; set; } = true;
}

/// <summary>
/// Excel import column mapping configuration
/// Allows flexible column matching for different D365 export formats
/// </summary>
public class ExcelImportSettings
{
    /// <summary>
    /// Number of columns to skip at the beginning (D365 metadata columns A, B, C)
    /// </summary>
    public int SkipFirstColumns { get; set; } = 3;

    // Ressourcen column mappings
    public ColumnMapping RessourceName { get; set; } = new("Ressourcenname", MatchType.Contains);
    public ColumnMapping RessourceBezirk { get; set; } = new("Bezirk", MatchType.StartsWith);

    // Bereitschaftsgruppen column mappings
    public ColumnMapping GruppenName { get; set; } = new("Name", MatchType.Contains);
    public ColumnMapping GruppenBezirk { get; set; } = new("Bezirk", MatchType.StartsWith);
    public ColumnMapping GruppenVerantwortlich { get; set; } = new("Verantwortliche Person", MatchType.Contains);
}

/// <summary>
/// Column mapping configuration for a single field
/// </summary>
public class ColumnMapping
{
    /// <summary>
    /// Search term to find the column
    /// </summary>
    public string SearchTerm { get; set; }

    /// <summary>
    /// How to match the column name
    /// </summary>
    public MatchType MatchType { get; set; }

    public ColumnMapping() : this(string.Empty, MatchType.Contains) { }

    public ColumnMapping(string searchTerm, MatchType matchType)
    {
        SearchTerm = searchTerm;
        MatchType = matchType;
    }
}

/// <summary>
/// Column name matching strategy
/// </summary>
public enum MatchType
{
    /// <summary>
    /// Column name must contain the search term (case-insensitive)
    /// </summary>
    Contains,

    /// <summary>
    /// Column name must start with the search term (case-insensitive)
    /// </summary>
    StartsWith,

    /// <summary>
    /// Column name must exactly match the search term (case-insensitive)
    /// </summary>
    Exact
}
