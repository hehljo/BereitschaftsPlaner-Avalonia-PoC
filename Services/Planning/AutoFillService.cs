using System;
using System.Collections.Generic;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.Services.Planning;

/// <summary>
/// Service for automatic fair distribution of resources across shifts
/// </summary>
public class AutoFillService
{
    private readonly Random _random = new();

    /// <summary>
    /// Auto-fill a month with fair resource distribution
    /// </summary>
    public List<PlanningAssignment> AutoFillMonth(
        DateTime month,
        List<BereitschaftsGruppe> groups,
        List<Ressource> ressourcen,
        string typ,
        Dictionary<string, List<DateTime>>? vacationDays = null)
    {
        var assignments = new List<PlanningAssignment>();

        if (groups.Count == 0 || ressourcen.Count == 0)
        {
            Log.Warning("AutoFill: No groups or resources available");
            return assignments;
        }

        try
        {
            // Get all days in month
            var firstDay = new DateTime(month.Year, month.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            var totalDays = (int)(lastDay - firstDay).TotalDays + 1;

            // Calculate fair distribution
            var shiftsPerPerson = CalculateFairDistribution(totalDays, ressourcen.Count);

            // Track assignments per person for fairness
            var assignmentCounts = ressourcen.ToDictionary(r => r.Name, r => 0);
            var lastAssignmentDate = ressourcen.ToDictionary(r => r.Name, r => DateTime.MinValue);

            // Process each group
            foreach (var group in groups)
            {
                // Shuffle resources for randomness
                var shuffledResources = ressourcen.OrderBy(_ => _random.Next()).ToList();

                // Assign for each day in month
                for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
                {
                    // Find best resource for this day
                    var bestResource = FindBestResourceForDay(
                        day,
                        shuffledResources,
                        assignmentCounts,
                        lastAssignmentDate,
                        vacationDays,
                        shiftsPerPerson
                    );

                    if (bestResource != null)
                    {
                        var assignment = new PlanningAssignment
                        {
                            Date = day,
                            GruppeName = group.Name,
                            RessourceName = bestResource.Name,
                            Typ = typ,
                            StartZeit = typ == "BD" ? "16:00" : "07:30",
                            EndZeit = typ == "BD" ? "07:30" : "16:00"
                        };

                        assignments.Add(assignment);
                        assignmentCounts[bestResource.Name]++;
                        lastAssignmentDate[bestResource.Name] = day;
                    }
                }
            }

            Log.Information("AutoFill completed: {Count} assignments created for {Month}",
                assignments.Count, month.ToString("yyyy-MM"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AutoFill failed");
        }

        return assignments;
    }

    /// <summary>
    /// Find best resource for a specific day considering fairness
    /// </summary>
    private Ressource? FindBestResourceForDay(
        DateTime day,
        List<Ressource> availableResources,
        Dictionary<string, int> assignmentCounts,
        Dictionary<string, DateTime> lastAssignmentDate,
        Dictionary<string, List<DateTime>>? vacationDays,
        int targetShiftsPerPerson)
    {
        // Filter out unavailable resources
        var candidates = availableResources.Where(r =>
        {
            // Check vacation
            if (vacationDays != null &&
                vacationDays.TryGetValue(r.Name, out var vacation) &&
                vacation.Contains(day.Date))
            {
                return false;
            }

            // Don't assign same person consecutive days (if possible)
            if (lastAssignmentDate[r.Name] == day.AddDays(-1))
            {
                // Only allow if all others are also consecutive
                var anyNonConsecutive = availableResources.Any(other =>
                    lastAssignmentDate[other.Name] != day.AddDays(-1));

                if (anyNonConsecutive)
                    return false;
            }

            return true;
        }).ToList();

        if (candidates.Count == 0)
        {
            // Fallback: use all resources
            candidates = availableResources;
        }

        // Score each candidate by fairness
        var scored = candidates.Select(r => new
        {
            Resource = r,
            Score = CalculateFairnessScore(
                assignmentCounts[r.Name],
                targetShiftsPerPerson,
                lastAssignmentDate[r.Name],
                day
            )
        }).OrderByDescending(x => x.Score).ToList();

        return scored.FirstOrDefault()?.Resource;
    }

    /// <summary>
    /// Calculate fairness score (higher = better candidate)
    /// </summary>
    private double CalculateFairnessScore(
        int currentAssignments,
        int targetAssignments,
        DateTime lastAssignment,
        DateTime currentDay)
    {
        double score = 0;

        // Primary: Under target = higher priority
        var distanceFromTarget = targetAssignments - currentAssignments;
        score += distanceFromTarget * 100;

        // Secondary: Days since last assignment
        var daysSinceLastAssignment = (currentDay - lastAssignment).TotalDays;
        score += daysSinceLastAssignment * 10;

        // Bonus for weekends if under target
        if (currentDay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            if (currentAssignments < targetAssignments)
                score += 5;
        }

        return score;
    }

    /// <summary>
    /// Calculate how many shifts each person should get
    /// </summary>
    private int CalculateFairDistribution(int totalDays, int resourceCount)
    {
        if (resourceCount == 0) return 0;

        // Round up to ensure full coverage
        return (int)Math.Ceiling((double)totalDays / resourceCount);
    }

    /// <summary>
    /// Get fairness statistics for current assignments
    /// </summary>
    public FairnessStats GetFairnessStats(
        List<PlanningAssignment> assignments,
        List<Ressource> allResources)
    {
        var stats = new FairnessStats();

        if (allResources.Count == 0)
            return stats;

        // Count assignments per person
        var counts = allResources.ToDictionary(r => r.Name, r => 0);
        foreach (var assignment in assignments)
        {
            if (counts.ContainsKey(assignment.RessourceName))
            {
                counts[assignment.RessourceName]++;
            }
        }

        var allCounts = counts.Values.ToList();

        stats.MinShifts = allCounts.Count > 0 ? allCounts.Min() : 0;
        stats.MaxShifts = allCounts.Count > 0 ? allCounts.Max() : 0;
        stats.AverageShifts = allCounts.Count > 0 ? allCounts.Average() : 0;
        stats.StandardDeviation = CalculateStandardDeviation(allCounts);
        stats.FairnessScore = CalculateOverallFairnessScore(allCounts);
        stats.PersonStats = counts.Select(kvp => new PersonFairnessInfo
        {
            Name = kvp.Key,
            ShiftCount = kvp.Value,
            DeviationFromAverage = kvp.Value - stats.AverageShifts
        }).OrderByDescending(p => p.ShiftCount).ToList();

        return stats;
    }

    private double CalculateStandardDeviation(List<int> values)
    {
        if (values.Count == 0) return 0;

        var avg = values.Average();
        var sumOfSquares = values.Sum(val => Math.Pow(val - avg, 2));
        return Math.Sqrt(sumOfSquares / values.Count);
    }

    private double CalculateOverallFairnessScore(List<int> values)
    {
        if (values.Count == 0) return 100;

        var max = values.Max();
        var min = values.Min();
        var range = max - min;

        // Perfect fairness = 100%, larger range = lower score
        // Range of 0 = 100%, range of 5 = ~75%, range of 10 = ~50%
        var score = Math.Max(0, 100 - (range * 5));

        return score;
    }
}

/// <summary>
/// Fairness statistics for resource distribution
/// </summary>
public class FairnessStats
{
    public int MinShifts { get; set; }
    public int MaxShifts { get; set; }
    public double AverageShifts { get; set; }
    public double StandardDeviation { get; set; }
    public double FairnessScore { get; set; } // 0-100, higher is better
    public List<PersonFairnessInfo> PersonStats { get; set; } = new();

    public string FairnessLevel => FairnessScore switch
    {
        >= 95 => "Perfekt",
        >= 85 => "Sehr gut",
        >= 70 => "Gut",
        >= 50 => "Akzeptabel",
        _ => "Ungleichgewicht erkannt"
    };

    public string GetSummary()
    {
        return $"{FairnessLevel} ({FairnessScore:F0}%) | " +
               $"Ø {AverageShifts:F1} Dienste | " +
               $"Min: {MinShifts} | Max: {MaxShifts}";
    }
}

/// <summary>
/// Per-person fairness information
/// </summary>
public class PersonFairnessInfo
{
    public string Name { get; set; } = string.Empty;
    public int ShiftCount { get; set; }
    public double DeviationFromAverage { get; set; }

    public string Status => DeviationFromAverage switch
    {
        > 2 => "⚠️ Überlastet",
        < -2 => "💤 Unterlastet",
        _ => "✅ Ausgeglichen"
    };
}
