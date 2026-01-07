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
