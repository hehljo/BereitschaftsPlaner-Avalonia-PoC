using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services;
using BereitschaftsPlaner.Avalonia.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class GeneratorViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;
    private readonly ZeitprofilService _zeitprofilService;
    private readonly BereitschaftsExcelService _excelService;

    public GeneratorViewModel()
    {
        _dbService = App.DatabaseService;
        _zeitprofilService = App.ZeitprofilService;
        _excelService = new BereitschaftsExcelService();

        // Load data from database
        LoadDataFromDatabase();

        // Set default date range (current month)
        StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);

        // Update statistics when dates change
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(StartDate) ||
                e.PropertyName == nameof(EndDate) ||
                e.PropertyName == nameof(SelectedGruppen))
            {
                UpdateStatistics();
            }
        };
    }

    // ============================================================================
    // DATA COLLECTIONS
    // ============================================================================

    [ObservableProperty]
    private ObservableCollection<string> _bezirke = new();

    [ObservableProperty]
    private ObservableCollection<BereitschaftsGruppe> _alleGruppen = new();

    [ObservableProperty]
    private ObservableCollection<BereitschaftsGruppe> _filteredGruppen = new();

    [ObservableProperty]
    private ObservableCollection<BereitschaftsGruppe> _selectedGruppen = new();

    [ObservableProperty]
    private ObservableCollection<Ressource> _ressourcen = new();

    // ============================================================================
    // FILTER & SELECTION
    // ============================================================================

    [ObservableProperty]
    private string _selectedBezirk = "Alle";

    [ObservableProperty]
    private string _gruppenSuchtext = string.Empty;

    [ObservableProperty]
    private Ressource? _selectedRessource;

    // ============================================================================
    // DATE RANGE
    // ============================================================================

    [ObservableProperty]
    private DateTime _startDate;

    [ObservableProperty]
    private DateTime _endDate;

    // ============================================================================
    // STATISTICS & STATUS
    // ============================================================================

    [ObservableProperty]
    private string _statisticsText = "0 Gruppen ausgewählt";

    [ObservableProperty]
    private string _statusMessage = "Bereit";

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private int _totalEntries = 0;

    [ObservableProperty]
    private bool _isGenerating = false;

    // ============================================================================
    // DATA LOADING
    // ============================================================================

    private void LoadDataFromDatabase()
    {
        try
        {
            // Load Gruppen
            var gruppenFromDb = _dbService.GetAllBereitschaftsGruppen();
            AlleGruppen.Clear();
            foreach (var gruppe in gruppenFromDb)
            {
                AlleGruppen.Add(gruppe);
            }

            // Extract unique Bezirke
            Bezirke.Clear();
            Bezirke.Add("Alle");
            var uniqueBezirke = gruppenFromDb
                .Select(g => g.Bezirk)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct()
                .OrderBy(b => b);
            foreach (var bezirk in uniqueBezirke)
            {
                Bezirke.Add(bezirk);
            }

            // Load Ressourcen
            var ressourcenFromDb = _dbService.GetAllRessourcen();
            Ressourcen.Clear();
            foreach (var ressource in ressourcenFromDb)
            {
                Ressourcen.Add(ressource);
            }

            // Auto-select first resource
            if (Ressourcen.Count > 0)
            {
                SelectedRessource = Ressourcen[0];
            }

            // Apply initial filter
            ApplyFilter();

            SetStatus($"{AlleGruppen.Count} Gruppen, {Ressourcen.Count} Ressourcen geladen", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Laden: {ex.Message}", Brushes.Red);
        }
    }

    // ============================================================================
    // FILTER LOGIC
    // ============================================================================

    partial void OnSelectedBezirkChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnGruppenSuchtextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredGruppen.Clear();

        var filtered = AlleGruppen.AsEnumerable();

        // Bezirksfilter
        if (SelectedBezirk != "Alle")
        {
            filtered = filtered.Where(g => g.Bezirk == SelectedBezirk);
        }

        // Suchtext-Filter
        if (!string.IsNullOrWhiteSpace(GruppenSuchtext))
        {
            filtered = filtered.Where(g =>
                g.Name.Contains(GruppenSuchtext, StringComparison.OrdinalIgnoreCase) ||
                (g.Bezirk?.Contains(GruppenSuchtext, StringComparison.OrdinalIgnoreCase) ?? false)
            );
        }

        // Sort
        filtered = filtered.OrderBy(g => g.Name);

        foreach (var gruppe in filtered)
        {
            FilteredGruppen.Add(gruppe);
        }

        SetStatus($"{FilteredGruppen.Count} Gruppen gefiltert", Brushes.Blue);
    }

    // ============================================================================
    // SELECTION MANAGEMENT
    // ============================================================================

    [RelayCommand]
    private void AddGruppe(BereitschaftsGruppe gruppe)
    {
        if (!SelectedGruppen.Contains(gruppe))
        {
            SelectedGruppen.Add(gruppe);
            UpdateStatistics();
        }
    }

    [RelayCommand]
    private void RemoveGruppe(BereitschaftsGruppe gruppe)
    {
        SelectedGruppen.Remove(gruppe);
        UpdateStatistics();
    }

    [RelayCommand]
    private void SelectAllFiltered()
    {
        foreach (var gruppe in FilteredGruppen)
        {
            if (!SelectedGruppen.Contains(gruppe))
            {
                SelectedGruppen.Add(gruppe);
            }
        }
        UpdateStatistics();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedGruppen.Clear();
        UpdateStatistics();
    }

    // ============================================================================
    // STATISTICS
    // ============================================================================

    private void UpdateStatistics()
    {
        var anzahlGruppen = SelectedGruppen.Count;
        var anzahlTage = (EndDate - StartDate).Days + 1;

        if (anzahlTage < 1) anzahlTage = 0;

        TotalEntries = anzahlGruppen * anzahlTage;

        if (anzahlGruppen == 0)
        {
            StatisticsText = "⚠️ Bitte mindestens eine Gruppe auswählen";
            StatusColor = Brushes.Orange;
        }
        else
        {
            StatisticsText = $"✅ {anzahlGruppen} Gruppen × {anzahlTage} Tage = ca. {TotalEntries} Einträge";
            StatusColor = Brushes.Green;
        }
    }

    // ============================================================================
    // GENERATION
    // ============================================================================

    [RelayCommand]
    private async Task Generate()
    {
        // Validation
        if (SelectedGruppen.Count == 0)
        {
            SetStatus("Bitte mindestens eine Gruppe auswählen!", Brushes.Orange);
            return;
        }

        if (SelectedRessource == null)
        {
            SetStatus("Bitte eine Ressource auswählen!", Brushes.Orange);
            return;
        }

        if (EndDate < StartDate)
        {
            SetStatus("Enddatum muss nach oder gleich Startdatum liegen!", Brushes.Orange);
            return;
        }

        // Get save location
        var mainWindow = App.MainWindow;
        if (mainWindow?.StorageProvider == null)
        {
            SetStatus("Fehler: Hauptfenster nicht verfügbar", Brushes.Red);
            return;
        }

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Bereitschaftsdienste speichern",
            DefaultExtension = "xlsx",
            SuggestedFileName = $"Bereitschaften_{StartDate:yyyyMMdd}-{EndDate:yyyyMMdd}.xlsx",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Excel Dateien") { Patterns = new[] { "*.xlsx" } },
                new FilePickerFileType("Alle Dateien") { Patterns = new[] { "*.*" } }
            }
        });

        if (file == null)
        {
            SetStatus("Abgebrochen", Brushes.Gray);
            return;
        }

        var outputPath = file.Path.LocalPath;

        IsGenerating = true;
        SetStatus("Generierung gestartet...", Brushes.Blue);

        try
        {
            var result = await Task.Run(() =>
            {
                return _excelService.GenerateBereitschaften(
                    outputPath,
                    SelectedGruppen.ToList(),
                    SelectedRessource,
                    StartDate,
                    EndDate,
                    _zeitprofilService,
                    App.FeiertagsService,
                    progressCallback: (current, total, message) =>
                    {
                        // Update status on UI thread
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            SetStatus($"{message} ({current}/{total})", Brushes.Blue);
                        });
                    }
                );
            });

            if (result.Success)
            {
                SetStatus($"✅ {result.Message}\n📄 Gespeichert: {Path.GetFileName(outputPath)}", Brushes.Green);
            }
            else
            {
                SetStatus($"❌ {result.Message}", Brushes.Red);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler bei Generierung: {ex.Message}", Brushes.Red);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    // ============================================================================
    // QUICK ACTIONS
    // ============================================================================

    [RelayCommand]
    private void SetDateRangeCurrentMonth()
    {
        StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);
        SetStatus("Datumsbereich: Aktueller Monat", Brushes.Blue);
    }

    [RelayCommand]
    private void SetDateRangeNextMonth()
    {
        var nextMonth = DateTime.Now.AddMonths(1);
        StartDate = new DateTime(nextMonth.Year, nextMonth.Month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);
        SetStatus("Datumsbereich: Nächster Monat", Brushes.Blue);
    }

    [RelayCommand]
    private void SetDateRangeCurrentQuarter()
    {
        var now = DateTime.Now;
        var quarter = (now.Month - 1) / 3;
        StartDate = new DateTime(now.Year, quarter * 3 + 1, 1);
        EndDate = StartDate.AddMonths(3).AddDays(-1);
        SetStatus("Datumsbereich: Aktuelles Quartal", Brushes.Blue);
    }

    // ============================================================================
    // HELPERS
    // ============================================================================

    private void SetStatus(string message, IBrush color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}
