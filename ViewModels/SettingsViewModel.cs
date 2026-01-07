using System;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    private string _statusMessage = "Einstellungen werden geladen...";

    public SettingsViewModel()
    {
        _settingsService = App.SettingsService;
        _settings = _settingsService.LoadSettings();
        LoadFeatureFlags();
        StatusMessage = "Bereit";
    }

    // ============================================================================
    // TIER 1: Core Features
    // ============================================================================

    [ObservableProperty]
    private bool _autoFillEnabled;

    [ObservableProperty]
    private bool _fairnessDashboardEnabled;

    [ObservableProperty]
    private bool _vacationCalendarEnabled;

    // ============================================================================
    // TIER 2: Quality-of-Life
    // ============================================================================

    [ObservableProperty]
    private bool _historicalAnalysisEnabled;

    [ObservableProperty]
    private bool _shiftSwapEnabled;

    [ObservableProperty]
    private bool _conflictAssistantEnabled;

    [ObservableProperty]
    private bool _templateLibraryEnabled;

    // ============================================================================
    // TIER 3: Professional
    // ============================================================================

    [ObservableProperty]
    private bool _workloadHeatmapEnabled;

    [ObservableProperty]
    private bool _skillsMatchingEnabled;

    [ObservableProperty]
    private bool _multiTeamCoordinationEnabled;

    // ============================================================================
    // TIER 4: Integration
    // ============================================================================

    [ObservableProperty]
    private bool _notificationSystemEnabled;

    [ObservableProperty]
    private bool _calendarIntegrationEnabled;

    [ObservableProperty]
    private bool _mobileViewEnabled;

    // ============================================================================
    // TIER 5: Advanced Intelligence
    // ============================================================================

    [ObservableProperty]
    private bool _fairnessRulesEnabled;

    [ObservableProperty]
    private bool _whatIfScenariosEnabled;

    // ============================================================================
    // METHODS
    // ============================================================================

    private void LoadFeatureFlags()
    {
        var flags = _settings.Features;

        // Tier 1
        AutoFillEnabled = flags.AutoFillEnabled;
        FairnessDashboardEnabled = flags.FairnessDashboardEnabled;
        VacationCalendarEnabled = flags.VacationCalendarEnabled;

        // Tier 2
        HistoricalAnalysisEnabled = flags.HistoricalAnalysisEnabled;
        ShiftSwapEnabled = flags.ShiftSwapEnabled;
        ConflictAssistantEnabled = flags.ConflictAssistantEnabled;
        TemplateLibraryEnabled = flags.TemplatLibraryEnabled;

        // Tier 3
        WorkloadHeatmapEnabled = flags.WorkloadHeatmapEnabled;
        SkillsMatchingEnabled = flags.SkillsMatchingEnabled;
        MultiTeamCoordinationEnabled = flags.MultiTeamCoordinationEnabled;

        // Tier 4
        NotificationSystemEnabled = flags.NotificationSystemEnabled;
        CalendarIntegrationEnabled = flags.CalendarIntegrationEnabled;
        MobileViewEnabled = flags.MobileViewEnabled;

        // Tier 5
        FairnessRulesEnabled = flags.FairnessRulesEnabled;
        WhatIfScenariosEnabled = flags.WhatIfScenariosEnabled;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            // Update settings object
            _settings.Features.AutoFillEnabled = AutoFillEnabled;
            _settings.Features.FairnessDashboardEnabled = FairnessDashboardEnabled;
            _settings.Features.VacationCalendarEnabled = VacationCalendarEnabled;
            _settings.Features.HistoricalAnalysisEnabled = HistoricalAnalysisEnabled;
            _settings.Features.ShiftSwapEnabled = ShiftSwapEnabled;
            _settings.Features.ConflictAssistantEnabled = ConflictAssistantEnabled;
            _settings.Features.TemplatLibraryEnabled = TemplateLibraryEnabled;
            _settings.Features.WorkloadHeatmapEnabled = WorkloadHeatmapEnabled;
            _settings.Features.SkillsMatchingEnabled = SkillsMatchingEnabled;
            _settings.Features.MultiTeamCoordinationEnabled = MultiTeamCoordinationEnabled;
            _settings.Features.NotificationSystemEnabled = NotificationSystemEnabled;
            _settings.Features.CalendarIntegrationEnabled = CalendarIntegrationEnabled;
            _settings.Features.MobileViewEnabled = MobileViewEnabled;
            _settings.Features.FairnessRulesEnabled = FairnessRulesEnabled;
            _settings.Features.WhatIfScenariosEnabled = WhatIfScenariosEnabled;

            // Save to file
            _settingsService.SaveSettings(_settings);

            StatusMessage = "✅ Einstellungen gespeichert";
            Log.Information("Feature flags saved successfully");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Speichern: {ex.Message}";
            Log.Error(ex, "Failed to save feature flags");
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        // Tier 1: All enabled
        AutoFillEnabled = true;
        FairnessDashboardEnabled = true;
        VacationCalendarEnabled = true;

        // Tier 2: Mostly enabled
        HistoricalAnalysisEnabled = true;
        ShiftSwapEnabled = false;
        ConflictAssistantEnabled = true;
        TemplateLibraryEnabled = true;

        // Tier 3: Conservative defaults
        WorkloadHeatmapEnabled = true;
        SkillsMatchingEnabled = false;
        MultiTeamCoordinationEnabled = false;

        // Tier 4: Minimal integration
        NotificationSystemEnabled = false;
        CalendarIntegrationEnabled = true;
        MobileViewEnabled = false;

        // Tier 5: Enabled
        FairnessRulesEnabled = true;
        WhatIfScenariosEnabled = true;

        StatusMessage = "Auf Standard zurückgesetzt (noch nicht gespeichert)";
    }
}
