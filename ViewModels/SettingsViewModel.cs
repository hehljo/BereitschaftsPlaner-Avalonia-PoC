using System;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services;
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
            _settingsService.UpdateSetting<AppSettings>(s =>
            {
                // Tier 1
                s.Features.AutoFillEnabled = AutoFillEnabled;
                s.Features.FairnessDashboardEnabled = FairnessDashboardEnabled;
                s.Features.VacationCalendarEnabled = VacationCalendarEnabled;

                // Tier 2
                s.Features.HistoricalAnalysisEnabled = HistoricalAnalysisEnabled;
                s.Features.ShiftSwapEnabled = ShiftSwapEnabled;
                s.Features.ConflictAssistantEnabled = ConflictAssistantEnabled;
                s.Features.TemplatLibraryEnabled = TemplateLibraryEnabled;

                // Tier 3
                s.Features.WorkloadHeatmapEnabled = WorkloadHeatmapEnabled;
                s.Features.SkillsMatchingEnabled = SkillsMatchingEnabled;
                s.Features.MultiTeamCoordinationEnabled = MultiTeamCoordinationEnabled;

                // Tier 4
                s.Features.NotificationSystemEnabled = NotificationSystemEnabled;
                s.Features.CalendarIntegrationEnabled = CalendarIntegrationEnabled;
                s.Features.MobileViewEnabled = MobileViewEnabled;

                // Tier 5
                s.Features.FairnessRulesEnabled = FairnessRulesEnabled;
                s.Features.WhatIfScenariosEnabled = WhatIfScenariosEnabled;
            });

            StatusMessage = "Einstellungen gespeichert";
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
