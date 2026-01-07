using System;
using System.Collections.Generic;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Data;
using LiteDB;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.Services.Planning;

/// <summary>
/// Service for analyzing historical assignment data
/// </summary>
public class HistoryAnalysisService
{
    private readonly string _dbPath;

    public HistoryAnalysisService(DatabaseService dbService)
    {
        _dbPath = dbService.DatabasePath;
    }

    /// <summary>
    /// Generate 3-month report
    /// </summary>
    public HistoryReport Generate3MonthReport(DateTime referenceDate)
    {
        var endDate = new DateTime(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month));
        var startDate = endDate.AddMonths(-2).AddDays(-(endDate.Day - 1)); // Go back 3 months to the 1st

        return GenerateReport(startDate, endDate, "3-Monats-Report");
    }

    /// <summary>
    /// Generate 6-month report
    /// </summary>
    public HistoryReport Generate6MonthReport(DateTime referenceDate)
    {
        var endDate = new DateTime(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month));
        var startDate = endDate.AddMonths(-5).AddDays(-(endDate.Day - 1)); // Go back 6 months to the 1st

        return GenerateReport(startDate, endDate, "6-Monats-Report");
    }

    /// <summary>
    /// Generate 12-month (yearly) report
    /// </summary>
    public HistoryReport Generate12MonthReport(DateTime referenceDate)
    {
        var endDate = new DateTime(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month));
        var startDate = endDate.AddMonths(-11).AddDays(-(endDate.Day - 1)); // Go back 12 months to the 1st

        return GenerateReport(startDate, endDate, "12-Monats-Report");
    }

    /// <summary>
    /// Generate report for custom date range
    /// </summary>
    private HistoryReport GenerateReport(DateTime startDate, DateTime endDate, string reportType)
    {
        try
        {
            // Fetch all assignments in date range from database
            var assignments = GetAssignmentsInRange(startDate, endDate);

            var report = new HistoryReport
            {
                StartDate = startDate,
                EndDate = endDate,
                ReportType = reportType,
                TotalShifts = assignments.Count,
                TotalDays = (endDate - startDate).Days + 1
            };

            // Group by person
            var personGroups = assignments.GroupBy(a => a.RessourceName);

            foreach (var group in personGroups)
            {
                var personHistory = new PersonHistory
                {
                    Name = group.Key,
                    TotalShifts = group.Count(),
                    BDCount = group.Count(a => a.Typ == "BD"),
                    TDCount = group.Count(a => a.Typ == "TD"),
                    WeekendShifts = group.Count(a => a.Date.DayOfWeek == DayOfWeek.Saturday || a.Date.DayOfWeek == DayOfWeek.Sunday),
                    HolidayShifts = 0 // TODO: Integrate with FeiertagsService
                };

                // Group by month for trend data
                var monthGroups = group.GroupBy(a => a.Date.ToString("yyyy-MM"));
                foreach (var monthGroup in monthGroups)
                {
                    personHistory.ShiftsByMonth[monthGroup.Key] = monthGroup.Count();
                }

                report.PersonHistories.Add(personHistory);
            }

            // Aggregate shifts by month
            var monthlyGroups = assignments.GroupBy(a => a.Date.ToString("yyyy-MM"));
            foreach (var monthGroup in monthlyGroups)
            {
                report.ShiftsByMonth[monthGroup.Key] = monthGroup.Count();
            }

            // Aggregate by type
            var typeGroups = assignments.GroupBy(a => a.Typ);
            foreach (var typeGroup in typeGroups)
            {
                report.ShiftsByType[typeGroup.Key] = typeGroup.Count();
            }

            Log.Information("Historical report generated: {Type}, {Count} shifts",
                reportType, report.TotalShifts);

            return report;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler bei Generierung des Historical Reports");
            return new HistoryReport
            {
                StartDate = startDate,
                EndDate = endDate,
                ReportType = reportType
            };
        }
    }

    /// <summary>
    /// Get all assignments in date range from database
    /// NOTE: This assumes we're storing PlanningAssignments in LiteDB
    /// If not, this will need to query the Excel exports or another data source
    /// </summary>
    private List<PlanningAssignment> GetAssignmentsInRange(DateTime start, DateTime end)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningAssignment>("planningAssignments");

            return collection
                .Find(a => a.Date >= start && a.Date <= end)
                .OrderBy(a => a.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Fehler beim Laden der Assignments aus DB - Collection existiert möglicherweise nicht");
            return new List<PlanningAssignment>();
        }
    }

    /// <summary>
    /// Save assignments to database for historical tracking
    /// Call this after exporting to Excel or when planning is finalized
    /// </summary>
    public void SaveAssignmentsForHistory(List<PlanningAssignment> assignments)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningAssignment>("planningAssignments");

            // Ensure index on Date
            collection.EnsureIndex(x => x.Date);
            collection.EnsureIndex(x => x.RessourceName);

            foreach (var assignment in assignments)
            {
                // Check if already exists (by Date + GruppeName + Typ)
                var existing = collection.FindOne(x =>
                    x.Date.Date == assignment.Date.Date &&
                    x.GruppeName == assignment.GruppeName &&
                    x.Typ == assignment.Typ
                );

                if (existing != null)
                {
                    // Update
                    existing.RessourceName = assignment.RessourceName;
                    existing.StartZeit = assignment.StartZeit;
                    existing.EndZeit = assignment.EndZeit;
                    collection.Update(existing);
                }
                else
                {
                    // Insert
                    collection.Insert(assignment);
                }
            }

            Log.Information("Saved {Count} assignments for historical tracking", assignments.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Speichern der Assignments für History");
            throw;
        }
    }

    /// <summary>
    /// Get comparison data for multiple persons
    /// </summary>
    public Dictionary<string, PersonHistory> GetPersonComparison(DateTime start, DateTime end, List<string> personNames)
    {
        var assignments = GetAssignmentsInRange(start, end);
        var result = new Dictionary<string, PersonHistory>();

        foreach (var personName in personNames)
        {
            var personAssignments = assignments.Where(a => a.RessourceName == personName).ToList();

            var history = new PersonHistory
            {
                Name = personName,
                TotalShifts = personAssignments.Count,
                BDCount = personAssignments.Count(a => a.Typ == "BD"),
                TDCount = personAssignments.Count(a => a.Typ == "TD"),
                WeekendShifts = personAssignments.Count(a =>
                    a.Date.DayOfWeek == DayOfWeek.Saturday ||
                    a.Date.DayOfWeek == DayOfWeek.Sunday)
            };

            result[personName] = history;
        }

        return result;
    }
}
