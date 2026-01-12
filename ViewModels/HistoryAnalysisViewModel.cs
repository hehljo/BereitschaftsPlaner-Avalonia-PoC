using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Planning;
using BereitschaftsPlaner.Avalonia.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private async Task ShowDetailedReport()
    {
        if (CurrentReport == null)
        {
            var warningDialog = new ConfirmDialog(
                "Kein Report",
                "Bitte erst einen Report generieren.",
                "OK",
                ""
            );
            await warningDialog.ShowDialog<bool>(App.MainWindow!);
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

        var reportDialog = new ConfirmDialog(
            "Detaillierter Report",
            sb.ToString(),
            "OK",
            ""
        );
        await reportDialog.ShowDialog<bool>(App.MainWindow!);
    }

    /// <summary>
    /// Export report to CSV (simple implementation)
    /// </summary>
    [RelayCommand]
    private async Task ExportToCSV()
    {
        if (CurrentReport == null)
        {
            var warningDialog = new ConfirmDialog(
                "Kein Report",
                "Bitte erst einen Report generieren.",
                "OK",
                ""
            );
            await warningDialog.ShowDialog<bool>(App.MainWindow!);
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

            var successDialog = new ConfirmDialog(
                "Export erfolgreich",
                $"Report exportiert nach:\n{csvPath}",
                "OK",
                ""
            );
            await successDialog.ShowDialog<bool>(App.MainWindow!);

            Log.Information("Report exportiert: {Path}", csvPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim CSV-Export");
            var errorDialog = new ConfirmDialog(
                "Fehler",
                $"Fehler beim Export: {ex.Message}",
                "OK",
                ""
            );
            await errorDialog.ShowDialog<bool>(App.MainWindow!);
        }
    }
}
