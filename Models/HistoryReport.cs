using System;
using System.Collections.Generic;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Historical analysis report for assignments over time
/// </summary>
public class HistoryReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalShifts { get; set; }
    public int TotalDays { get; set; }
    public string ReportType { get; set; } = string.Empty; // "3-Month", "6-Month", "12-Month"

    public List<PersonHistory> PersonHistories { get; set; } = new();
    public Dictionary<string, int> ShiftsByMonth { get; set; } = new(); // "2026-01" -> count
    public Dictionary<string, int> ShiftsByType { get; set; } = new(); // "BD" -> count, "TD" -> count

    /// <summary>
    /// Get summary text for display
    /// </summary>
    public string Summary =>
        $"{ReportType}: {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}\n" +
        $"Gesamt: {TotalShifts} Dienste über {TotalDays} Tage\n" +
        $"Durchschnitt: {(PersonHistories.Count > 0 ? TotalShifts / (double)PersonHistories.Count : 0):F1} Dienste/Person";
}

/// <summary>
/// Historical data for a single person
/// </summary>
public class PersonHistory
{
    public string Name { get; set; } = string.Empty;
    public int TotalShifts { get; set; }
    public int BDCount { get; set; }
    public int TDCount { get; set; }
    public int WeekendShifts { get; set; }
    public int HolidayShifts { get; set; }

    public Dictionary<string, int> ShiftsByMonth { get; set; } = new(); // "2026-01" -> count

    /// <summary>
    /// Display text for UI
    /// </summary>
    public string DisplayText =>
        $"{Name}: {TotalShifts} Dienste (BD: {BDCount}, TD: {TDCount}, Wochenenden: {WeekendShifts})";

    /// <summary>
    /// Percentage of total shifts
    /// </summary>
    public double Percentage(int totalShifts) =>
        totalShifts > 0 ? (TotalShifts / (double)totalShifts) * 100 : 0;
}
