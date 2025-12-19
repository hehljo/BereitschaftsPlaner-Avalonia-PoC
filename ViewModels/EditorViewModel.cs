using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BereitschaftsPlaner.Avalonia.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    public EditorViewModel()
    {
        // Set default filter dates (current month)
        FilterStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        FilterEndDate = FilterStartDate.AddMonths(1).AddDays(-1);
    }

    // ============================================================================
    // FILE MANAGEMENT
    // ============================================================================

    [ObservableProperty]
    private string _excelFilePath = string.Empty;

    [ObservableProperty]
    private bool _hasLoadedData = false;

    // ============================================================================
    // DATA COLLECTIONS
    // ============================================================================

    [ObservableProperty]
    private ObservableCollection<BereitschaftEntry> _alleBereitschaften = new();

    [ObservableProperty]
    private ObservableCollection<BereitschaftEntry> _filteredBereitschaften = new();

    [ObservableProperty]
    private ObservableCollection<string> _gruppen = new();

    [ObservableProperty]
    private ObservableCollection<string> _ressourcen = new();

    // ============================================================================
    // FILTER OPTIONS
    // ============================================================================

    [ObservableProperty]
    private DateTime _filterStartDate;

    [ObservableProperty]
    private DateTime _filterEndDate;

    [ObservableProperty]
    private string _selectedGruppeFilter = "Alle";

    [ObservableProperty]
    private string _selectedRessourceFilter = "Alle";

    [ObservableProperty]
    private string _searchText = string.Empty;

    // ============================================================================
    // SELECTION
    // ============================================================================

    [ObservableProperty]
    private BereitschaftEntry? _selectedBereitschaft;

    [ObservableProperty]
    private ObservableCollection<BereitschaftEntry> _selectedBereitschaften = new();

    // ============================================================================
    // STATUS
    // ============================================================================

    [ObservableProperty]
    private string _statusMessage = "Bereit - Keine Datei geladen";

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isSaving = false;

    [ObservableProperty]
    private bool _hasUnsavedChanges = false;

    [ObservableProperty]
    private int _totalEntries = 0;

    [ObservableProperty]
    private int _filteredCount = 0;

    // ============================================================================
    // FILE OPERATIONS
    // ============================================================================

    [RelayCommand]
    private async Task BrowseFile()
    {
        try
        {
            var mainWindow = App.MainWindow;
            if (mainWindow?.StorageProvider == null)
            {
                SetStatus("Fehler: Hauptfenster nicht verfügbar", Brushes.Red);
                return;
            }

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Excel-Datei öffnen",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Excel Dateien") { Patterns = new[] { "*.xlsx", "*.xls" } },
                    new FilePickerFileType("Alle Dateien") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                ExcelFilePath = files[0].Path.LocalPath;
                SetStatus($"Datei ausgewählt: {System.IO.Path.GetFileName(ExcelFilePath)}", Brushes.Blue);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler bei Dateiauswahl: {ex.Message}", Brushes.Red);
        }
    }

    [RelayCommand]
    private async Task LoadExcel()
    {
        if (string.IsNullOrWhiteSpace(ExcelFilePath))
        {
            SetStatus("Bitte zuerst eine Excel-Datei auswählen", Brushes.Orange);
            return;
        }

        if (!System.IO.File.Exists(ExcelFilePath))
        {
            SetStatus("Datei nicht gefunden!", Brushes.Red);
            return;
        }

        IsLoading = true;
        SetStatus("Lade Excel-Datei...", Brushes.Blue);

        try
        {
            // TODO: Implement actual Excel import
            // This will require ExcelDataReader or similar
            await Task.Delay(1000); // Simulate work

            // Mock data for now
            AlleBereitschaften.Clear();
            Gruppen.Clear();
            Ressourcen.Clear();

            Gruppen.Add("Alle");
            Ressourcen.Add("Alle");

            // Example: Create some mock entries
            for (int i = 0; i < 10; i++)
            {
                AlleBereitschaften.Add(new BereitschaftEntry
                {
                    Id = i + 1,
                    Datum = DateTime.Now.AddDays(i),
                    GruppeName = $"Gruppe {(i % 3) + 1}",
                    RessourceName = $"Mitarbeiter {(i % 5) + 1}",
                    StartZeit = "16:00",
                    EndZeit = "07:30",
                    Typ = i % 2 == 0 ? "BD" : "TD"
                });
            }

            TotalEntries = AlleBereitschaften.Count;
            HasLoadedData = true;
            HasUnsavedChanges = false;

            ApplyFilter();

            SetStatus($"✅ {TotalEntries} Einträge geladen", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Laden: {ex.Message}", Brushes.Red);
            HasLoadedData = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveExcel()
    {
        if (!HasLoadedData)
        {
            SetStatus("Keine Daten zum Speichern", Brushes.Orange);
            return;
        }

        if (!HasUnsavedChanges)
        {
            SetStatus("Keine Änderungen zum Speichern", Brushes.Blue);
            return;
        }

        IsSaving = true;
        SetStatus("Speichere Änderungen...", Brushes.Blue);

        try
        {
            // TODO: Implement actual Excel save
            await Task.Delay(1000); // Simulate work

            HasUnsavedChanges = false;
            SetStatus($"✅ Änderungen gespeichert: {ExcelFilePath}", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Speichern: {ex.Message}", Brushes.Red);
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ============================================================================
    // FILTER LOGIC
    // ============================================================================

    partial void OnFilterStartDateChanged(DateTime value)
    {
        ApplyFilter();
    }

    partial void OnFilterEndDateChanged(DateTime value)
    {
        ApplyFilter();
    }

    partial void OnSelectedGruppeFilterChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedRessourceFilterChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (!HasLoadedData)
        {
            FilteredBereitschaften.Clear();
            FilteredCount = 0;
            return;
        }

        var filtered = AlleBereitschaften.AsEnumerable();

        // Date range filter
        filtered = filtered.Where(b =>
            b.Datum >= FilterStartDate &&
            b.Datum <= FilterEndDate
        );

        // Gruppe filter
        if (SelectedGruppeFilter != "Alle")
        {
            filtered = filtered.Where(b => b.GruppeName == SelectedGruppeFilter);
        }

        // Ressource filter
        if (SelectedRessourceFilter != "Alle")
        {
            filtered = filtered.Where(b => b.RessourceName == SelectedRessourceFilter);
        }

        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(b =>
                b.GruppeName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                b.RessourceName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                b.Typ.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            );
        }

        // Sort by date
        filtered = filtered.OrderBy(b => b.Datum).ThenBy(b => b.GruppeName);

        FilteredBereitschaften.Clear();
        foreach (var entry in filtered)
        {
            FilteredBereitschaften.Add(entry);
        }

        FilteredCount = FilteredBereitschaften.Count;
        SetStatus($"{FilteredCount} von {TotalEntries} Einträgen angezeigt", Brushes.Blue);
    }

    // ============================================================================
    // EDIT OPERATIONS
    // ============================================================================

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedBereitschaften.Count == 0)
        {
            SetStatus("Keine Einträge ausgewählt", Brushes.Orange);
            return;
        }

        var count = SelectedBereitschaften.Count;

        foreach (var entry in SelectedBereitschaften.ToList())
        {
            AlleBereitschaften.Remove(entry);
            FilteredBereitschaften.Remove(entry);
        }

        SelectedBereitschaften.Clear();
        TotalEntries = AlleBereitschaften.Count;
        HasUnsavedChanges = true;

        SetStatus($"✅ {count} Einträge gelöscht", Brushes.Orange);
        ApplyFilter();
    }

    [RelayCommand]
    private void DuplicateSelected()
    {
        if (SelectedBereitschaft == null)
        {
            SetStatus("Bitte einen Eintrag auswählen", Brushes.Orange);
            return;
        }

        var duplicate = new BereitschaftEntry
        {
            Id = AlleBereitschaften.Max(b => b.Id) + 1,
            Datum = SelectedBereitschaft.Datum.AddDays(1),
            GruppeName = SelectedBereitschaft.GruppeName,
            RessourceName = SelectedBereitschaft.RessourceName,
            StartZeit = SelectedBereitschaft.StartZeit,
            EndZeit = SelectedBereitschaft.EndZeit,
            Typ = SelectedBereitschaft.Typ
        };

        AlleBereitschaften.Add(duplicate);
        TotalEntries = AlleBereitschaften.Count;
        HasUnsavedChanges = true;

        SetStatus("✅ Eintrag dupliziert", Brushes.Green);
        ApplyFilter();
    }

    // ============================================================================
    // QUICK ACTIONS
    // ============================================================================

    [RelayCommand]
    private void SetFilterCurrentMonth()
    {
        FilterStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        FilterEndDate = FilterStartDate.AddMonths(1).AddDays(-1);
        SetStatus("Filter: Aktueller Monat", Brushes.Blue);
    }

    [RelayCommand]
    private void SetFilterNextMonth()
    {
        var nextMonth = DateTime.Now.AddMonths(1);
        FilterStartDate = new DateTime(nextMonth.Year, nextMonth.Month, 1);
        FilterEndDate = FilterStartDate.AddMonths(1).AddDays(-1);
        SetStatus("Filter: Nächster Monat", Brushes.Blue);
    }

    [RelayCommand]
    private void ClearAllFilters()
    {
        SelectedGruppeFilter = "Alle";
        SelectedRessourceFilter = "Alle";
        SearchText = string.Empty;
        SetFilterCurrentMonth();
        SetStatus("Alle Filter zurückgesetzt", Brushes.Blue);
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

// ============================================================================
// BEREITSCHAFT ENTRY MODEL
// ============================================================================

public partial class BereitschaftEntry : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private DateTime _datum;

    [ObservableProperty]
    private string _gruppeName = string.Empty;

    [ObservableProperty]
    private string _ressourceName = string.Empty;

    [ObservableProperty]
    private string _startZeit = string.Empty;

    [ObservableProperty]
    private string _endZeit = string.Empty;

    [ObservableProperty]
    private string _typ = string.Empty; // "BD" or "TD"
}
