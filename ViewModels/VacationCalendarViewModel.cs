using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class VacationCalendarViewModel : ViewModelBase
{
    private readonly VacationCalendarService _vacationService;

    [ObservableProperty]
    private ObservableCollection<Ressource> _availableResources = new();

    [ObservableProperty]
    private Ressource? _selectedResource;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private VacationType _selectedType = VacationType.Vacation;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VacationDay> _vacationDays = new();

    [ObservableProperty]
    private string _statusMessage = "Bereit";

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    public VacationCalendarViewModel()
    {
        _vacationService = App.VacationCalendarService;
        LoadResources();
        LoadVacationDays();
    }

    private void LoadResources()
    {
        var resources = App.DatabaseService.GetAllRessourcen();
        AvailableResources.Clear();
        foreach (var res in resources)
        {
            AvailableResources.Add(res);
        }

        if (AvailableResources.Count > 0)
        {
            SelectedResource = AvailableResources[0];
        }
    }

    private void LoadVacationDays()
    {
        try
        {
            var start = DateTime.Today.AddMonths(-1);
            var end = DateTime.Today.AddMonths(3);

            var days = _vacationService.GetVacationDaysInRange(start, end);

            VacationDays.Clear();
            foreach (var day in days)
            {
                VacationDays.Add(day);
            }

            SetStatus($"{days.Count} Urlaubstage geladen", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to load vacation days");
        }
    }

    [RelayCommand]
    private void AddVacation()
    {
        if (SelectedResource == null)
        {
            SetStatus("Bitte Ressource auswählen", Brushes.Orange);
            return;
        }

        if (EndDate < StartDate)
        {
            SetStatus("End-Datum muss nach Start-Datum liegen", Brushes.Orange);
            return;
        }

        try
        {
            _vacationService.AddVacationRange(
                SelectedResource.Name,
                StartDate,
                EndDate,
                SelectedType,
                string.IsNullOrWhiteSpace(Note) ? null : Note
            );

            LoadVacationDays();

            var days = (int)(EndDate - StartDate).TotalDays + 1;
            SetStatus($"✅ {days} Urlaubstag(e) hinzugefügt", Brushes.Green);

            // Reset form
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(1);
            Note = string.Empty;
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to add vacation");
        }
    }

    [RelayCommand]
    private void DeleteSelected(VacationDay? vacation)
    {
        if (vacation == null) return;

        try
        {
            _vacationService.RemoveVacationDay(vacation.Id);
            VacationDays.Remove(vacation);
            SetStatus("✅ Urlaubstag gelöscht", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to delete vacation");
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        try
        {
            _vacationService.ClearAllVacationDays();
            LoadVacationDays();
            SetStatus("✅ Alle Urlaubstage gelöscht", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler: {ex.Message}", Brushes.Red);
            Log.Error(ex, "Failed to clear vacations");
        }
    }

    private void SetStatus(string message, IBrush color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}
