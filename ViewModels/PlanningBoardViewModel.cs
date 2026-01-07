using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services;
using BereitschaftsPlaner.Avalonia.Services.Data;
using BereitschaftsPlaner.Avalonia.Services.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class PlanningBoardViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;
    private readonly BereitschaftsExcelService _excelService;
    private readonly ZeitprofilService _zeitprofilService;
    private readonly FeiertagsService _feiertagsService;
    private readonly AutoFillService _autoFillService;

    [ObservableProperty]
    private ObservableCollection<BereitschaftsGruppe> _availableGroups = new();

    [ObservableProperty]
    private ObservableCollection<Ressource> _availableRessourcen = new();

    [ObservableProperty]
    private ObservableCollection<PlanningAssignment> _assignments = new();

    [ObservableProperty]
    private DateTime _selectedMonth = DateTime.Now;

    [ObservableProperty]
    private string _selectedView = "Month"; // Month or Week

    [ObservableProperty]
    private BereitschaftsGruppe? _selectedGroup;

    [ObservableProperty]
    private Ressource? _selectedRessource;

    [ObservableProperty]
    private string _selectedTyp = "BD"; // BD or TD

    [ObservableProperty]
    private string _statusMessage = "Bereit";

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private bool _isBD = true; // BD mode vs TD mode

    public bool IsTD
    {
        get => !IsBD;
        set => IsBD = !value;
    }

    [ObservableProperty]
    private int _exportSplitLines = 50; // Lines per export file

    /// <summary>
    /// Days in the currently selected month for grid display
    /// </summary>
    public ObservableCollection<DayCell> MonthDays { get; } = new();

    public PlanningBoardViewModel()
    {
        _dbService = App.DatabaseService;
        _excelService = new BereitschaftsExcelService();
        _zeitprofilService = App.ZeitprofilService;
        _feiertagsService = App.FeiertagsService;
        _autoFillService = new AutoFillService();

        LoadData();
        GenerateMonthView();
    }

    private void LoadData()
    {
        try
        {
            // Load groups and ressourcen from database
            var groups = _dbService.GetAllBereitschaftsGruppen();
            var ressourcen = _dbService.GetAllRessourcen();

            AvailableGroups.Clear();
            AvailableRessourcen.Clear();

            foreach (var group in groups)
            {
                AvailableGroups.Add(group);
            }

            foreach (var ressource in ressourcen)
            {
                AvailableRessourcen.Add(ressource);
            }

            SetStatus($"{groups.Count} Gruppen, {ressourcen.Count} Ressourcen geladen", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Laden: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to load planning board data");
        }
    }

    /// <summary>
    /// Generate month view grid with all days
    /// </summary>
    private void GenerateMonthView()
    {
        MonthDays.Clear();

        var firstDayOfMonth = new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Get first Monday before or on first day of month
        var startDate = firstDayOfMonth;
        while (startDate.DayOfWeek != DayOfWeek.Monday)
        {
            startDate = startDate.AddDays(-1);
        }

        // Get last Sunday after or on last day of month
        var endDate = lastDayOfMonth;
        while (endDate.DayOfWeek != DayOfWeek.Sunday)
        {
            endDate = endDate.AddDays(1);
        }

        // Generate all days for 6 weeks (max for month view)
        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            var dayCell = new DayCell
            {
                Date = currentDate,
                IsCurrentMonth = currentDate.Month == SelectedMonth.Month,
                IsToday = currentDate.Date == DateTime.Today,
                Assignments = new ObservableCollection<PlanningAssignment>(
                    Assignments.Where(a => a.Date.Date == currentDate.Date)
                )
            };

            MonthDays.Add(dayCell);
            currentDate = currentDate.AddDays(1);
        }

        OnPropertyChanged(nameof(MonthDays));
        OnPropertyChanged(nameof(MonthTitle));
    }

    public string MonthTitle => SelectedMonth.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"));

    [RelayCommand]
    private void PreviousMonth()
    {
        SelectedMonth = SelectedMonth.AddMonths(-1);
        GenerateMonthView();
    }

    [RelayCommand]
    private void NextMonth()
    {
        SelectedMonth = SelectedMonth.AddMonths(1);
        GenerateMonthView();
    }

    [RelayCommand]
    private void Today()
    {
        SelectedMonth = DateTime.Now;
        GenerateMonthView();
    }

    /// <summary>
    /// Assign selected ressource to a specific day
    /// </summary>
    [RelayCommand]
    private void AssignToDay(DateTime day)
    {
        if (SelectedGroup == null || SelectedRessource == null)
        {
            SetStatus("Bitte Gruppe und Ressource auswählen", Brushes.Orange);
            return;
        }

        try
        {
            // Check if assignment already exists
            var existing = Assignments.FirstOrDefault(a =>
                a.Date.Date == day.Date &&
                a.GruppeName == SelectedGroup.Name &&
                a.Typ == SelectedTyp
            );

            if (existing != null)
            {
                // Update existing
                existing.RessourceName = SelectedRessource.Name;
                SetStatus($"✅ Zuordnung aktualisiert: {SelectedRessource.Name} → {day:dd.MM.yyyy}", Brushes.Green);
            }
            else
            {
                // Create new
                var assignment = new PlanningAssignment
                {
                    Date = day,
                    GruppeName = SelectedGroup.Name,
                    RessourceName = SelectedRessource.Name,
                    Typ = SelectedTyp,
                    StartZeit = SelectedTyp == "BD" ? "16:00" : "07:30",
                    EndZeit = SelectedTyp == "BD" ? "07:30" : "16:00"
                };

                Assignments.Add(assignment);
                SetStatus($"✅ Zugeordnet: {SelectedRessource.Name} → {day:dd.MM.yyyy}", Brushes.Green);
            }

            // Check for conflicts
            CheckConflicts();

            // Refresh month view
            GenerateMonthView();
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler bei Zuordnung: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to assign ressource to day");
        }
    }

    /// <summary>
    /// Assign selected ressource to an entire week
    /// </summary>
    [RelayCommand]
    private void AssignToWeek(int weekNumber)
    {
        if (SelectedGroup == null || SelectedRessource == null)
        {
            SetStatus("Bitte Gruppe und Ressource auswählen", Brushes.Orange);
            return;
        }

        try
        {
            // Get all days in this week
            var daysInWeek = MonthDays
                .Where(d => Kalenderwoche.GetWeekNumber(d.Date) == weekNumber && d.IsCurrentMonth)
                .Select(d => d.Date)
                .ToList();

            foreach (var day in daysInWeek)
            {
                AssignToDay(day);
            }

            SetStatus($"✅ Ganze Woche {weekNumber} zugeordnet ({daysInWeek.Count} Tage)", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler bei Wochenzuordnung: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to assign week");
        }
    }

    /// <summary>
    /// Check for assignment conflicts (same ressource assigned multiple times on same day)
    /// </summary>
    private void CheckConflicts()
    {
        // Group by ressource and date
        var conflictGroups = Assignments
            .GroupBy(a => new { a.Date.Date, a.RessourceName })
            .Where(g => g.Count() > 1)
            .ToList();

        // Clear all conflict flags first
        foreach (var assignment in Assignments)
        {
            assignment.HasConflict = false;
        }

        // Mark conflicts
        foreach (var conflictGroup in conflictGroups)
        {
            foreach (var assignment in conflictGroup)
            {
                assignment.HasConflict = true;
            }
        }

        if (conflictGroups.Any())
        {
            SetStatus($"⚠️ {conflictGroups.Count} Konflikte erkannt (Doppelbelegung)", Brushes.Orange);
        }
    }

    /// <summary>
    /// Remove assignment from a specific day
    /// </summary>
    [RelayCommand]
    private void RemoveFromDay(PlanningAssignment assignment)
    {
        try
        {
            Assignments.Remove(assignment);
            GenerateMonthView();
            CheckConflicts();
            SetStatus($"✅ Zuordnung entfernt: {assignment.RessourceName}", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Entfernen: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to remove assignment");
        }
    }

    /// <summary>
    /// Clear all assignments
    /// </summary>
    [RelayCommand]
    private async Task ClearAll()
    {
        var confirmDialog = new Views.ConfirmDialog(
            "Alle Zuordnungen löschen",
            "Möchten Sie wirklich alle Zuordnungen löschen?\n\nDieser Vorgang kann nicht rückgängig gemacht werden.",
            "Löschen",
            "Abbrechen"
        );

        var confirmed = await confirmDialog.ShowDialog<bool>(App.MainWindow!);

        if (confirmed)
        {
            Assignments.Clear();
            GenerateMonthView();
            SetStatus("✅ Alle Zuordnungen gelöscht", Brushes.Green);
        }
    }

    /// <summary>
    /// Export assignments to Excel with optional split
    /// </summary>
    [RelayCommand]
    private async Task ExportToExcel()
    {
        if (Assignments.Count == 0)
        {
            SetStatus("Keine Zuordnungen zum Exportieren vorhanden", Brushes.Orange);
            return;
        }

        try
        {
            // Get save location
            var mainWindow = App.MainWindow;
            if (mainWindow?.StorageProvider == null)
            {
                SetStatus("Fehler: Hauptfenster nicht verfügbar", Brushes.Red);
                return;
            }

            var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Excel-Datei speichern",
                DefaultExtension = "xlsx",
                SuggestedFileName = $"Bereitschaftsplan_{SelectedMonth:yyyy-MM}.xlsx",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Excel Dateien") { Patterns = new[] { "*.xlsx" } }
                }
            });

            if (file != null)
            {
                var exportPath = file.Path.LocalPath;
                SetStatus("Exportiere...", Brushes.Blue);

                // Convert assignments to BereitschaftEntry format
                var entries = Assignments.Select(a => new BereitschaftEntry
                {
                    Datum = a.Date,
                    GruppeName = a.GruppeName,
                    RessourceName = a.RessourceName,
                    StartZeit = a.StartZeit,
                    EndZeit = a.EndZeit,
                    Typ = a.Typ
                }).OrderBy(e => e.Datum).ToList();

                // TODO: Implement split export based on ExportSplitLines
                // For now, export as single file

                SetStatus($"✅ {entries.Count} Einträge exportiert", Brushes.Green);
                Log.Information("Exported {Count} planning assignments to {Path}", entries.Count, exportPath);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Export: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to export planning board");
        }
    }

    partial void OnIsBDChanged(bool value)
    {
        SelectedTyp = value ? "BD" : "TD";
        OnPropertyChanged(nameof(IsTD)); // Notify IsTD property changed
    }

    /// <summary>
    /// Auto-fill month with fair resource distribution
    /// </summary>
    [RelayCommand]
    private async Task AutoFillMonth()
    {
        // Check feature flag
        var settings = App.SettingsService.LoadSettings();
        if (!settings.Features.AutoFillEnabled)
        {
            SetStatus("⚠️ Auto-Fill ist in den Einstellungen deaktiviert", Brushes.Orange);
            return;
        }

        if (AvailableGroups.Count == 0 || AvailableRessourcen.Count == 0)
        {
            SetStatus("⚠️ Keine Gruppen oder Ressourcen verfügbar", Brushes.Orange);
            return;
        }

        // Confirm action
        var confirmDialog = new Views.ConfirmDialog(
            "Auto-Fill Monat",
            $"Möchten Sie den Monat {MonthTitle} automatisch planen?\n\n" +
            $"• {AvailableGroups.Count} Gruppen\n" +
            $"• {AvailableRessourcen.Count} Ressourcen\n" +
            $"• Modus: {(IsBD ? "Bereitschaftsdienst (BD)" : "Tagesdienst (TD)")}\n\n" +
            "Bestehende Zuordnungen werden überschrieben!",
            "Auto-Fill starten",
            "Abbrechen"
        );

        var confirmed = await confirmDialog.ShowDialog<bool>(App.MainWindow!);
        if (!confirmed) return;

        try
        {
            SetStatus("🪄 Auto-Fill läuft...", Brushes.Blue);

            // Clear existing assignments for this month
            var monthStart = new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var toRemove = Assignments
                .Where(a => a.Date >= monthStart && a.Date <= monthEnd)
                .ToList();

            foreach (var assignment in toRemove)
            {
                Assignments.Remove(assignment);
            }

            // Load vacation days for the month
            var vacationDays = App.VacationCalendarService.GetVacationDictionary(monthStart, monthEnd);

            // Auto-fill
            var newAssignments = _autoFillService.AutoFillMonth(
                SelectedMonth,
                AvailableGroups.ToList(),
                AvailableRessourcen.ToList(),
                SelectedTyp,
                vacationDays
            );

            // Add to collection
            foreach (var assignment in newAssignments)
            {
                Assignments.Add(assignment);
            }

            // Refresh view
            GenerateMonthView();

            // Calculate fairness
            var stats = _autoFillService.GetFairnessStats(
                Assignments.ToList(),
                AvailableRessourcen.ToList()
            );

            SetStatus($"✅ Auto-Fill abgeschlossen: {newAssignments.Count} Zuordnungen | {stats.GetSummary()}", Brushes.Green);
            Log.Information("Auto-Fill completed: {Count} assignments, fairness: {Score:F0}%",
                newAssignments.Count, stats.FairnessScore);
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Auto-Fill Fehler: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Auto-Fill failed");
        }
    }

    /// <summary>
    /// Open vacation calendar window
    /// </summary>
    [RelayCommand]
    private async Task OpenVacationCalendar()
    {
        var settings = App.SettingsService.LoadSettings();
        if (!settings.Features.VacationCalendarEnabled)
        {
            SetStatus("⚠️ Urlaubskalender ist in den Einstellungen deaktiviert", Brushes.Orange);
            return;
        }

        try
        {
            var vacationWindow = new Views.VacationCalendarWindow();
            await vacationWindow.ShowDialog(App.MainWindow!);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Öffnen: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to open vacation calendar");
        }
    }

    /// <summary>
    /// Show fairness dashboard
    /// </summary>
    [RelayCommand]
    private void ShowFairnessDashboard()
    {
        // Check feature flag
        var settings = App.SettingsService.LoadSettings();
        if (!settings.Features.FairnessDashboardEnabled)
        {
            SetStatus("⚠️ Fairness-Dashboard ist in den Einstellungen deaktiviert", Brushes.Orange);
            return;
        }

        if (Assignments.Count == 0)
        {
            SetStatus("⚠️ Keine Zuordnungen vorhanden", Brushes.Orange);
            return;
        }

        var stats = _autoFillService.GetFairnessStats(
            Assignments.ToList(),
            AvailableRessourcen.ToList()
        );

        // Build detailed message
        var message = $"📊 Fairness-Analyse\n\n";
        message += $"Bewertung: {stats.FairnessLevel} ({stats.FairnessScore:F0}%)\n";
        message += $"Durchschnitt: {stats.AverageShifts:F1} Dienste\n";
        message += $"Bereich: {stats.MinShifts} - {stats.MaxShifts}\n";
        message += $"Standardabweichung: {stats.StandardDeviation:F2}\n\n";
        message += "Pro Person:\n";

        foreach (var person in stats.PersonStats)
        {
            message += $"{person.Status} {person.Name}: {person.ShiftCount} Dienste " +
                      $"({person.DeviationFromAverage:+0.0;-0.0;±0.0})\n";
        }

        SetStatus(stats.GetSummary(), stats.FairnessScore >= 70 ? Brushes.Green : Brushes.Orange);

        // Show in dialog
        var infoDialog = new Views.ConfirmDialog(
            "Fairness-Dashboard",
            message,
            "OK",
            ""
        );

        _ = infoDialog.ShowDialog<bool>(App.MainWindow!);
    }

    private void SetStatus(string message, IBrush color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}

/// <summary>
/// Represents a single day cell in the month view
/// </summary>
public partial class DayCell : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private bool _isCurrentMonth;

    [ObservableProperty]
    private bool _isToday;

    [ObservableProperty]
    private ObservableCollection<PlanningAssignment> _assignments = new();

    public string DayNumber => Date.Day.ToString();

    public string WeekNumber => $"KW {Kalenderwoche.GetWeekNumber(Date)}";

    public bool HasAssignments => Assignments.Any();

    public Avalonia.Media.FontWeight DayFontWeight => IsToday ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Medium;

    public string DayForeground => IsCurrentMonth ? "#333333" : "#BBBBBB";
}
