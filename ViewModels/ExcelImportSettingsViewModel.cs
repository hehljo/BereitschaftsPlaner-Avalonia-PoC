using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaThreading = Avalonia.Threading;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class ExcelImportSettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    public event Action? CloseRequested;

    // Skip Columns Setting
    [ObservableProperty]
    private int _skipFirstColumns;

    // Ressourcen Mappings
    [ObservableProperty]
    private string _ressourcenNameSearchTerm = string.Empty;

    [ObservableProperty]
    private int _ressourcenNameMatchType;

    [ObservableProperty]
    private string _ressourcenBezirkSearchTerm = string.Empty;

    [ObservableProperty]
    private int _ressourcenBezirkMatchType;

    // Gruppen Mappings
    [ObservableProperty]
    private string _gruppenNameSearchTerm = string.Empty;

    [ObservableProperty]
    private int _gruppenNameMatchType;

    [ObservableProperty]
    private string _gruppenBezirkSearchTerm = string.Empty;

    [ObservableProperty]
    private int _gruppenBezirkMatchType;

    [ObservableProperty]
    private string _gruppenVerantwortlichSearchTerm = string.Empty;

    [ObservableProperty]
    private int _gruppenVerantwortlichMatchType;

    // Status Message
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Color _statusColor = Colors.Gray;

    public ObservableCollection<string> MatchTypes { get; } = new()
    {
        "Contains (Enthält)",
        "StartsWith (Beginnt mit)",
        "Exact (Exakt)"
    };

    public ExcelImportSettingsViewModel()
    {
        _settingsService = App.SettingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.GetSettings().ExcelImport;

        SkipFirstColumns = settings.SkipFirstColumns;

        // Ressourcen
        RessourcenNameSearchTerm = settings.RessourceName.SearchTerm;
        RessourcenNameMatchType = (int)settings.RessourceName.MatchType;

        RessourcenBezirkSearchTerm = settings.RessourceBezirk.SearchTerm;
        RessourcenBezirkMatchType = (int)settings.RessourceBezirk.MatchType;

        // Gruppen
        GruppenNameSearchTerm = settings.GruppenName.SearchTerm;
        GruppenNameMatchType = (int)settings.GruppenName.MatchType;

        GruppenBezirkSearchTerm = settings.GruppenBezirk.SearchTerm;
        GruppenBezirkMatchType = (int)settings.GruppenBezirk.MatchType;

        GruppenVerantwortlichSearchTerm = settings.GruppenVerantwortlich.SearchTerm;
        GruppenVerantwortlichMatchType = (int)settings.GruppenVerantwortlich.MatchType;
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var settings = _settingsService.GetSettings();

            settings.ExcelImport.SkipFirstColumns = SkipFirstColumns;

            // Ressourcen
            settings.ExcelImport.RessourceName.SearchTerm = RessourcenNameSearchTerm;
            settings.ExcelImport.RessourceName.MatchType = (MatchType)RessourcenNameMatchType;

            settings.ExcelImport.RessourceBezirk.SearchTerm = RessourcenBezirkSearchTerm;
            settings.ExcelImport.RessourceBezirk.MatchType = (MatchType)RessourcenBezirkMatchType;

            // Gruppen
            settings.ExcelImport.GruppenName.SearchTerm = GruppenNameSearchTerm;
            settings.ExcelImport.GruppenName.MatchType = (MatchType)GruppenNameMatchType;

            settings.ExcelImport.GruppenBezirk.SearchTerm = GruppenBezirkSearchTerm;
            settings.ExcelImport.GruppenBezirk.MatchType = (MatchType)GruppenBezirkMatchType;

            settings.ExcelImport.GruppenVerantwortlich.SearchTerm = GruppenVerantwortlichSearchTerm;
            settings.ExcelImport.GruppenVerantwortlich.MatchType = (MatchType)GruppenVerantwortlichMatchType;

            _settingsService.SaveSettings(settings);

            StatusMessage = "✓ Einstellungen erfolgreich gespeichert";
            StatusColor = Colors.Green;

            Serilog.Log.Information("Excel import settings saved successfully");

            // Close dialog after 1 second
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
            {
                AvaloniaThreading.Dispatcher.UIThread.Post(() => CloseRequested?.Invoke());
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Fehler beim Speichern: {ex.Message}";
            StatusColor = Colors.Red;
            Serilog.Log.Error(ex, "Failed to save Excel import settings");
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaults = new ExcelImportSettings();

        SkipFirstColumns = defaults.SkipFirstColumns;

        // Ressourcen
        RessourcenNameSearchTerm = defaults.RessourceName.SearchTerm;
        RessourcenNameMatchType = (int)defaults.RessourceName.MatchType;

        RessourcenBezirkSearchTerm = defaults.RessourceBezirk.SearchTerm;
        RessourcenBezirkMatchType = (int)defaults.RessourceBezirk.MatchType;

        // Gruppen
        GruppenNameSearchTerm = defaults.GruppenName.SearchTerm;
        GruppenNameMatchType = (int)defaults.GruppenName.MatchType;

        GruppenBezirkSearchTerm = defaults.GruppenBezirk.SearchTerm;
        GruppenBezirkMatchType = (int)defaults.GruppenBezirk.MatchType;

        GruppenVerantwortlichSearchTerm = defaults.GruppenVerantwortlich.SearchTerm;
        GruppenVerantwortlichMatchType = (int)defaults.GruppenVerantwortlich.MatchType;

        StatusMessage = "Standard-Einstellungen wiederhergestellt";
        StatusColor = Colors.Orange;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke();
    }
}
