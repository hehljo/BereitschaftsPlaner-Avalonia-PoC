using System;
using System.Collections.Generic;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.Services.Planning;

/// <summary>
/// Enhanced conflict detection service
/// Detects:
/// - Double assignments
/// - Overload (>3 consecutive shifts)
/// - Vacation conflicts
/// - Skills mismatches (future)
/// </summary>
public class ConflictDetectionService
{
    private readonly VacationCalendarService _vacationService;

    public ConflictDetectionService(VacationCalendarService vacationService)
    {
        _vacationService = vacationService;
    }

    /// <summary>
    /// Detect all conflicts in a list of assignments
    /// </summary>
    public ConflictReport DetectConflicts(List<PlanningAssignment> assignments, List<Ressource> allResources)
    {
        var report = new ConflictReport();

        // 1. Double assignment detection
        var doubleAssignments = DetectDoubleAssignments(assignments);
        report.DoubleAssignments.AddRange(doubleAssignments);

        // 2. Overload detection (>3 consecutive shifts)
        var overloads = DetectOverload(assignments);
        report.Overloads.AddRange(overloads);

        // 3. Vacation conflicts
        var vacationConflicts = DetectVacationConflicts(assignments);
        report.VacationConflicts.AddRange(vacationConflicts);

        // 4. Unbalanced workload
        var workloadIssues = DetectWorkloadImbalance(assignments, allResources);
        report.WorkloadIssues.AddRange(workloadIssues);

        report.TotalConflicts = report.DoubleAssignments.Count +
                                 report.Overloads.Count +
                                 report.VacationConflicts.Count +
                                 report.WorkloadIssues.Count;

        Log.Information("Conflict detection: {Total} conflicts found", report.TotalConflicts);

        return report;
    }

    /// <summary>
    /// Detect double assignments (same person on same day)
    /// </summary>
    private List<ConflictDetail> DetectDoubleAssignments(List<PlanningAssignment> assignments)
    {
        var conflicts = new List<ConflictDetail>();

        var grouped = assignments.GroupBy(a => new { a.Date, a.RessourceName });

        foreach (var group in grouped.Where(g => g.Count() > 1))
        {
            conflicts.Add(new ConflictDetail
            {
                Type = ConflictType.DoubleAssignment,
                Date = group.Key.Date,
                RessourceName = group.Key.RessourceName,
                Description = $"{group.Key.RessourceName} hat {group.Count()} Zuordnungen am {group.Key.Date:dd.MM.yyyy}",
                Severity = ConflictSeverity.High,
                Suggestion = "Bitte eine der Zuordnungen entfernen"
            });
        }

        return conflicts;
    }

    /// <summary>
    /// Detect overload (>3 consecutive shifts)
    /// </summary>
    private List<ConflictDetail> DetectOverload(List<PlanningAssignment> assignments)
    {
        var conflicts = new List<ConflictDetail>();

        var byPerson = assignments.GroupBy(a => a.RessourceName);

        foreach (var personGroup in byPerson)
        {
            var sortedDates = personGroup.Select(a => a.Date).OrderBy(d => d).ToList();

            int consecutiveCount = 1;
            DateTime? firstDate = null;

            for (int i = 0; i < sortedDates.Count - 1; i++)
            {
                if ((sortedDates[i + 1] - sortedDates[i]).Days == 1)
                {
                    if (consecutiveCount == 1)
                        firstDate = sortedDates[i];

                    consecutiveCount++;

                    if (consecutiveCount > 3)
                    {
                        conflicts.Add(new ConflictDetail
                        {
                            Type = ConflictType.Overload,
                            Date = sortedDates[i + 1],
                            RessourceName = personGroup.Key,
                            Description = $"{personGroup.Key} hat {consecutiveCount} aufeinanderfolgende Dienste (ab {firstDate:dd.MM.yyyy})",
                            Severity = ConflictSeverity.Medium,
                            Suggestion = "Berücksichtigen Sie Ruhezeiten"
                        });
                    }
                }
                else
                {
                    consecutiveCount = 1;
                    firstDate = null;
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detect vacation conflicts
    /// </summary>
    private List<ConflictDetail> DetectVacationConflicts(List<PlanningAssignment> assignments)
    {
        var conflicts = new List<ConflictDetail>();

        if (assignments.Count == 0)
            return conflicts;

        var minDate = assignments.Min(a => a.Date);
        var maxDate = assignments.Max(a => a.Date);

        var vacationDays = _vacationService.GetVacationDaysInRange(minDate, maxDate);

        foreach (var assignment in assignments)
        {
            var vacation = vacationDays.FirstOrDefault(v =>
                v.RessourceName == assignment.RessourceName &&
                v.Date.Date == assignment.Date.Date
            );

            if (vacation != null)
            {
                conflicts.Add(new ConflictDetail
                {
                    Type = ConflictType.VacationConflict,
                    Date = assignment.Date,
                    RessourceName = assignment.RessourceName,
                    Description = $"{assignment.RessourceName} ist am {assignment.Date:dd.MM.yyyy} als '{vacation.Type}' eingetragen",
                    Severity = ConflictSeverity.High,
                    Suggestion = $"Bitte andere Ressource zuweisen (Grund: {vacation.Type})"
                });
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detect workload imbalance
    /// </summary>
    private List<ConflictDetail> DetectWorkloadImbalance(List<PlanningAssignment> assignments, List<Ressource> allResources)
    {
        var conflicts = new List<ConflictDetail>();

        if (allResources.Count == 0)
            return conflicts;

        var assignmentCounts = assignments
            .GroupBy(a => a.RessourceName)
            .ToDictionary(g => g.Key, g => g.Count());

        var average = assignments.Count / (double)allResources.Count;
        var threshold = average * 0.5; // 50% deviation

        foreach (var resource in allResources)
        {
            var count = assignmentCounts.GetValueOrDefault(resource.Name, 0);

            if (count > average + threshold)
            {
                conflicts.Add(new ConflictDetail
                {
                    Type = ConflictType.WorkloadImbalance,
                    Date = DateTime.Now,
                    RessourceName = resource.Name,
                    Description = $"{resource.Name} ist überlastet: {count} Dienste (Durchschnitt: {average:F1})",
                    Severity = ConflictSeverity.Medium,
                    Suggestion = "Verwenden Sie Auto-Fill für gleichmäßige Verteilung"
                });
            }
            else if (count < average - threshold && count > 0)
            {
                conflicts.Add(new ConflictDetail
                {
                    Type = ConflictType.WorkloadImbalance,
                    Date = DateTime.Now,
                    RessourceName = resource.Name,
                    Description = $"{resource.Name} ist unterlastet: {count} Dienste (Durchschnitt: {average:F1})",
                    Severity = ConflictSeverity.Low,
                    Suggestion = "Mehr Dienste zuweisen für Fairness"
                });
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Get one-click fix suggestions for conflicts
    /// </summary>
    public List<ConflictFix> GetFixSuggestions(ConflictDetail conflict, List<Ressource> availableResources, List<PlanningAssignment> currentAssignments)
    {
        var fixes = new List<ConflictFix>();

        switch (conflict.Type)
        {
            case ConflictType.DoubleAssignment:
                // No automatic fix - user must manually remove one
                break;

            case ConflictType.VacationConflict:
                // Suggest available resources for that day
                var assignedOnDay = currentAssignments
                    .Where(a => a.Date.Date == conflict.Date.Date)
                    .Select(a => a.RessourceName)
                    .ToHashSet();

                var availableOnDay = availableResources
                    .Where(r => !assignedOnDay.Contains(r.Name))
                    .Where(r => _vacationService.IsResourceAvailable(r.Name, conflict.Date))
                    .ToList();

                foreach (var resource in availableOnDay.Take(3))
                {
                    fixes.Add(new ConflictFix
                    {
                        Description = $"Zuweisen zu {resource.Name}",
                        NewRessourceName = resource.Name
                    });
                }
                break;

            case ConflictType.Overload:
                // Suggest removing one of the consecutive assignments
                fixes.Add(new ConflictFix
                {
                    Description = "Entfernen Sie einen der aufeinanderfolgenden Dienste",
                    NewRessourceName = null
                });
                break;

            case ConflictType.WorkloadImbalance:
                // Suggest using Auto-Fill
                fixes.Add(new ConflictFix
                {
                    Description = "Verwenden Sie 'Auto-Fill' für automatische Verteilung",
                    NewRessourceName = null
                });
                break;
        }

        return fixes;
    }
}

/// <summary>
/// Conflict detection report
/// </summary>
public class ConflictReport
{
    public int TotalConflicts { get; set; }
    public List<ConflictDetail> DoubleAssignments { get; set; } = new();
    public List<ConflictDetail> Overloads { get; set; } = new();
    public List<ConflictDetail> VacationConflicts { get; set; } = new();
    public List<ConflictDetail> WorkloadIssues { get; set; } = new();

    public string Summary =>
        $"🚨 {TotalConflicts} Konflikte gefunden\n\n" +
        $"Doppelbelegungen: {DoubleAssignments.Count}\n" +
        $"Überlastungen: {Overloads.Count}\n" +
        $"Urlaubs-Konflikte: {VacationConflicts.Count}\n" +
        $"Arbeitsbelastung: {WorkloadIssues.Count}";

    public List<ConflictDetail> AllConflicts =>
        DoubleAssignments
            .Concat(Overloads)
            .Concat(VacationConflicts)
            .Concat(WorkloadIssues)
            .OrderByDescending(c => c.Severity)
            .ThenBy(c => c.Date)
            .ToList();
}

/// <summary>
/// Individual conflict detail
/// </summary>
public class ConflictDetail
{
    public ConflictType Type { get; set; }
    public DateTime Date { get; set; }
    public string RessourceName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ConflictSeverity Severity { get; set; }
    public string Suggestion { get; set; } = string.Empty;

    public string SeverityIcon => Severity switch
    {
        ConflictSeverity.High => "🔴",
        ConflictSeverity.Medium => "🟡",
        ConflictSeverity.Low => "🟢",
        _ => "⚪"
    };

    public string DisplayText =>
        $"{SeverityIcon} {Description}\n   → {Suggestion}";
}

/// <summary>
/// Suggested fix for a conflict
/// </summary>
public class ConflictFix
{
    public string Description { get; set; } = string.Empty;
    public string? NewRessourceName { get; set; }
}

public enum ConflictType
{
    DoubleAssignment,
    Overload,
    VacationConflict,
    SkillsMismatch,
    WorkloadImbalance
}

public enum ConflictSeverity
{
    Low,
    Medium,
    High,
    Critical
}
