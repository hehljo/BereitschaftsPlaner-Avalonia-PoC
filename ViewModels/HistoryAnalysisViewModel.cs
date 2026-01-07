using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class HistoryAnalysisViewModel : ViewModelBase
{
    private readonly HistoryAnalysisService _historyService;

    [ObservableProperty]
    private ObservableCollection<PersonHistory> _personHistories = new();

    [ObservableProperty]
    private HistoryReport? _currentReport;

    [ObservableProperty]
    private string _reportType = "3-Monats-Report";

    [ObservableProperty]
    private DateTime _referenceDate = DateTime.Now;

    public ObservableCollection<string> ReportTypes { get; } = new()
    {
        "3-Monats-Report",
        "6-Monats-Report",
        "12-Monats-Report"
    };

    public HistoryAnalysisViewModel()
    {
        _historyService = App.HistoryAnalysisService;
    }

    /// <summary>
    /// Generate report based on selected type
    /// </summary>
    [RelayCommand]
    private void GenerateReport()
    {
        try
        {
            CurrentReport = ReportType switch
            {
                "3-Monats-Report" => _historyService.Generate3MonthReport(ReferenceDate),
                "6-Monats-Report" => _historyService.Generate6MonthReport(ReferenceDate),
                "12-Monats-Report" => _historyService.Generate12MonthReport(ReferenceDate),
                _ => _historyService.Generate3MonthReport(ReferenceDate)
            };

            PersonHistories = new ObservableCollection<PersonHistory>(
                CurrentReport.PersonHistories.OrderByDescending(p => p.TotalShifts)
            );

            Log.Information("Report generiert: {Type}, {Count} Personen",
                ReportType, PersonHistories.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler bei Report-Generierung");
        }
    }

    /// <summary>
    /// Show detailed report as text
    /// </summary>
    [RelayCommand]
    private async void ShowDetailedReport()
    {
        if (CurrentReport == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Kein Report",
                "Bitte erst einen Report generieren.",
                ButtonEnum.Ok,
                Icon.Warning
            ).ShowAsync();
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"📊 {CurrentReport.ReportType}");
        sb.AppendLine($"Zeitraum: {CurrentReport.StartDate:dd.MM.yyyy} - {CurrentReport.EndDate:dd.MM.yyyy}");
        sb.AppendLine($"═══════════════════════════════════════");
        sb.AppendLine();

        sb.AppendLine($"Gesamt-Statistik:");
        sb.AppendLine($"  • Dienste gesamt: {CurrentReport.TotalShifts}");
        sb.AppendLine($"  • Tage: {CurrentReport.TotalDays}");
        sb.AppendLine($"  • Personen: {CurrentReport.PersonHistories.Count}");
        sb.AppendLine($"  • Ø Dienste/Person: {(CurrentReport.PersonHistories.Count > 0 ? CurrentReport.TotalShifts / (double)CurrentReport.PersonHistories.Count : 0):F1}");
        sb.AppendLine();

        if (CurrentReport.ShiftsByType.Any())
        {
            sb.AppendLine($"Dienst-Typen:");
            foreach (var kvp in CurrentReport.ShiftsByType)
            {
                sb.AppendLine($"  • {kvp.Key}: {kvp.Value} ({kvp.Value / (double)CurrentReport.TotalShifts * 100:F1}%)");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"Pro Person (Top 10):");
        var top10 = CurrentReport.PersonHistories.OrderByDescending(p => p.TotalShifts).Take(10);
        foreach (var person in top10)
        {
            sb.AppendLine($"  {person.Name}:");
            sb.AppendLine($"    • Gesamt: {person.TotalShifts} ({person.Percentage(CurrentReport.TotalShifts):F1}%)");
            sb.AppendLine($"    • BD: {person.BDCount} | TD: {person.TDCount}");
            sb.AppendLine($"    • Wochenenden: {person.WeekendShifts}");
        }

        await MessageBoxManager.GetMessageBoxStandard(
            "Detaillierter Report",
            sb.ToString(),
            ButtonEnum.Ok,
            Icon.Info
        ).ShowAsync();
    }

    /// <summary>
    /// Export report to CSV (simple implementation)
    /// </summary>
    [RelayCommand]
    private async void ExportToCSV()
    {
        if (CurrentReport == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Kein Report",
                "Bitte erst einen Report generieren.",
                ButtonEnum.Ok,
                Icon.Warning
            ).ShowAsync();
            return;
        }

        try
        {
            var csvPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"HistoryReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            );

            var sb = new StringBuilder();
            sb.AppendLine("Person;Gesamt;BD;TD;Wochenenden;Prozent");

            foreach (var person in CurrentReport.PersonHistories.OrderByDescending(p => p.TotalShifts))
            {
                sb.AppendLine($"{person.Name};{person.TotalShifts};{person.BDCount};{person.TDCount};{person.WeekendShifts};{person.Percentage(CurrentReport.TotalShifts):F1}");
            }

            System.IO.File.WriteAllText(csvPath, sb.ToString(), System.Text.Encoding.UTF8);

            await MessageBoxManager.GetMessageBoxStandard(
                "Export erfolgreich",
                $"Report exportiert nach:\n{csvPath}",
                ButtonEnum.Ok,
                Icon.Success
            ).ShowAsync();

            Log.Information("Report exportiert: {Path}", csvPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim CSV-Export");
            await MessageBoxManager.GetMessageBoxStandard(
                "Fehler",
                $"Fehler beim Export: {ex.Message}",
                ButtonEnum.Ok,
                Icon.Error
            ).ShowAsync();
        }
    }
}
