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

        // Initialize years and calendar weeks
        InitializeYears();
        LoadKalenderwochen();

        // Load data from database
        LoadDataFromDatabase();

        // Set default date range (current month)
        StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);

        // Update statistics when dates or selections change
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(StartDate) ||
                e.PropertyName == nameof(EndDate) ||
                e.PropertyName == nameof(SelectedGruppen) ||
                e.PropertyName == nameof(UseDateRange) ||
                e.PropertyName == nameof(Kalenderwochen))
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
    // DATE RANGE MODE
    // ============================================================================

    [ObservableProperty]
    private bool _useDateRange = true;

    public bool UseCalendarWeeks
    {
        get => !UseDateRange;
        set => UseDateRange = !value;
    }

    [ObservableProperty]
    private DateTime _startDate;

    [ObservableProperty]
    private DateTime _endDate;

    // ============================================================================
    // CALENDAR WEEKS MODE
    // ============================================================================

    [ObservableProperty]
    private int _selectedYear = DateTime.Now.Year;

    [ObservableProperty]
    private ObservableCollection<int> _availableYears = new();

    [ObservableProperty]
    private ObservableCollection<Kalenderwoche> _kalenderwochen = new();

    public int SelectedKWCount => Kalenderwochen?.Count(kw => kw.IsSelected) ?? 0;

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
    // CALENDAR WEEKS INITIALIZATION
    // ============================================================================

    private void InitializeYears()
    {
        AvailableYears.Clear();
        var currentYear = DateTime.Now.Year;
        
        for (int year = currentYear - 2; year <= currentYear + 2; year++)
        {
            AvailableYears.Add(year);
        }
        
        SelectedYear = currentYear;
    }

    partial void OnSelectedYearChanged(int value)
    {
        LoadKalenderwochen();
    }

    private void LoadKalenderwochen()
    {
        Kalenderwochen.Clear();
        
        var weeksInYear = Kalenderwoche.GetWeeksInYear(SelectedYear);
        
        for (int week = 1; week <= weeksInYear; week++)
        {
            var kw = new Kalenderwoche(SelectedYear, week);
            
            // Subscribe to property changes to update count
            kw.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Kalenderwoche.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedKWCount));
                    UpdateStatistics();
                }
            };
            
            Kalenderwochen.Add(kw);
        }
        
        OnPropertyChanged(nameof(SelectedKWCount));
    }

    [RelayCommand]
    private void ToggleAllKW()
    {
        var allSelected = Kalenderwochen.All(kw => kw.IsSelected);
        
        foreach (var kw in Kalenderwochen)
        {
            kw.IsSelected = !allSelected;
        }
        
        OnPropertyChanged(nameof(SelectedKWCount));
        UpdateStatistics();
    }

    [RelayCommand]
    private void SelectQuarter(int quarter)
    {
        // Deselect all first
        foreach (var kw in Kalenderwochen)
        {
            kw.IsSelected = false;
        }
        
        // Determine week range for quarter
        var (startWeek, endWeek) = quarter switch
        {
            1 => (1, 13),    // Q1: KW 1-13
            2 => (14, 26),   // Q2: KW 14-26
            3 => (27, 39),   // Q3: KW 27-39
            4 => (40, 53),   // Q4: KW 40-53
            _ => (1, 1)
        };
        
        foreach (var kw in Kalenderwochen.Where(kw => kw.Woche >= startWeek && kw.Woche <= endWeek))
        {
            kw.IsSelected = true;
        }
        
        OnPropertyChanged(nameof(SelectedKWCount));
        UpdateStatistics();
        SetStatus($"Quartal {quarter} ausgewählt", Brushes.Blue);
    }

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

        if (UseDateRange)
        {
            // Existing date range logic
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
        else // UseCalendarWeeks
        {
            var selectedKWs = Kalenderwochen.Where(kw => kw.IsSelected).ToList();
            var totalDays = selectedKWs.Sum(kw => (kw.EndDatum - kw.StartDatum).Days + 1);
            
            TotalEntries = anzahlGruppen * totalDays;

            if (anzahlGruppen == 0)
            {
                StatisticsText = "⚠️ Bitte mindestens eine Gruppe auswählen";
                StatusColor = Brushes.Orange;
            }
            else if (selectedKWs.Count == 0)
            {
                StatisticsText = "⚠️ Bitte mindestens eine Kalenderwoche auswählen";
                StatusColor = Brushes.Orange;
            }
            else
            {
                StatisticsText = $"✅ {anzahlGruppen} Gruppen × {selectedKWs.Count} KW ({totalDays} Tage) = ca. {TotalEntries} Einträge";
                StatusColor = Brushes.Green;
            }
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
            SetStatus("❌ Bitte mindestens eine Gruppe auswählen!", Brushes.Orange);
            return;
        }

        if (SelectedRessource == null)
        {
            SetStatus("❌ Bitte eine Ressource auswählen!", Brushes.Orange);
            return;
        }

        // Determine date range based on mode
        DateTime actualStartDate;
        DateTime actualEndDate;
        
        if (UseDateRange)
        {
            // Date range mode
            if (EndDate < StartDate)
            {
                SetStatus("❌ Enddatum muss nach oder gleich Startdatum liegen!", Brushes.Orange);
                return;
            }
            
            actualStartDate = StartDate;
            actualEndDate = EndDate;
        }
        else // UseCalendarWeeks
        {
            // Calendar weeks mode
            var selectedKWs = Kalenderwochen.Where(kw => kw.IsSelected).OrderBy(kw => kw.Woche).ToList();
            
            if (selectedKWs.Count == 0)
            {
                SetStatus("❌ Bitte mindestens eine Kalenderwoche auswählen!", Brushes.Orange);
                return;
            }
            
            actualStartDate = selectedKWs.First().StartDatum;
            actualEndDate = selectedKWs.Last().EndDatum;
        }

        // Get save location
        var mainWindow = App.MainWindow;
        if (mainWindow?.StorageProvider == null)
        {
            SetStatus("❌ Fehler: Hauptfenster nicht verfügbar", Brushes.Red);
            return;
        }

        var modeText = UseDateRange ? "Tage" : $"KW";
        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Bereitschaftsdienste speichern",
            DefaultExtension = "xlsx",
            SuggestedFileName = $"Bereitschaften_{actualStartDate:yyyyMMdd}-{actualEndDate:yyyyMMdd}.xlsx",
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
        SetStatus("⚡ Generierung gestartet...", Brushes.Blue);

        try
        {
            var result = await Task.Run(() =>
            {
                return _excelService.GenerateBereitschaften(
                    outputPath,
                    SelectedGruppen.ToList(),
                    SelectedRessource,
                    actualStartDate,
                    actualEndDate,
                    _zeitprofilService,
                    App.FeiertagsService,
                    progressCallback: (current, total, message) =>
                    {
                        // Update status on UI thread
                        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            var percentage = total > 0 ? (current * 100 / total) : 0;
                            SetStatus($"⚡ {message} ({percentage}%)", Brushes.Blue);
                        });
                    }
                );
            });

            if (result.Success)
            {
                var modeInfo = UseDateRange 
                    ? $"Zeitraum: {actualStartDate:dd.MM.yyyy} - {actualEndDate:dd.MM.yyyy}"
                    : $"Kalenderwochen: {SelectedKWCount} KW ausgewählt";
                    
                SetStatus($"✅ {result.Message}\n📄 {Path.GetFileName(outputPath)}\n📅 {modeInfo}", Brushes.Green);
            }
            else
            {
                SetStatus($"❌ {result.Message}", Brushes.Red);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Fehler bei Generierung: {ex.Message}", Brushes.Red);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    // ============================================================================
    // QUICK ACTIONS - DATE RANGE
    // ============================================================================

    [RelayCommand]
    private void SetDateRangeCurrentMonth()
    {
        StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);
        SetStatus("📅 Datumsbereich: Aktueller Monat", Brushes.Blue);
    }

    [RelayCommand]
    private void SetDateRangeNextMonth()
    {
        var nextMonth = DateTime.Now.AddMonths(1);
        StartDate = new DateTime(nextMonth.Year, nextMonth.Month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);
        SetStatus("📅 Datumsbereich: Nächster Monat", Brushes.Blue);
    }

    [RelayCommand]
    private void SetDateRangeCurrentQuarter()
    {
        var now = DateTime.Now;
        var quarter = (now.Month - 1) / 3;
        StartDate = new DateTime(now.Year, quarter * 3 + 1, 1);
        EndDate = StartDate.AddMonths(3).AddDays(-1);
        SetStatus("📅 Datumsbereich: Aktuelles Quartal", Brushes.Blue);
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
