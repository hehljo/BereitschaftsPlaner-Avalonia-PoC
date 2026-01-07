using System;
using System.Collections.Generic;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Data;
using LiteDB;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.Services.Planning;

/// <summary>
/// Service for managing planning scenarios (What-If analysis)
/// </summary>
public class ScenarioService
{
    private readonly string _dbPath;
    private readonly AutoFillService _autoFillService;

    public ScenarioService(DatabaseService dbService)
    {
        _dbPath = dbService.DatabasePath;
        _autoFillService = new AutoFillService();

        // Ensure indexes
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<PlanningScenario>("scenarios");
        collection.EnsureIndex(x => x.Name);
        collection.EnsureIndex(x => x.Month);
        collection.EnsureIndex(x => x.CreatedAt);
    }

    /// <summary>
    /// Save new scenario
    /// </summary>
    public void SaveScenario(PlanningScenario scenario)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningScenario>("scenarios");

            // Calculate fairness score before saving
            var stats = _autoFillService.GetFairnessStats(
                scenario.Assignments,
                scenario.Assignments.Select(a => new Ressource { Name = a.RessourceName }).DistinctBy(r => r.Name).ToList()
            );

            scenario.FairnessScore = stats.FairnessScore;
            scenario.ConflictCount = scenario.Assignments.Count(a => a.HasConflict);
            scenario.ModifiedAt = DateTime.Now;

            collection.Insert(scenario);
            Log.Information("Scenario gespeichert: {Name} ({Score}% fairness)",
                scenario.Name, scenario.FairnessScore);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Speichern des Scenarios");
            throw;
        }
    }

    /// <summary>
    /// Update existing scenario
    /// </summary>
    public void UpdateScenario(PlanningScenario scenario)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningScenario>("scenarios");

            // Recalculate metrics
            var stats = _autoFillService.GetFairnessStats(
                scenario.Assignments,
                scenario.Assignments.Select(a => new Ressource { Name = a.RessourceName }).DistinctBy(r => r.Name).ToList()
            );

            scenario.FairnessScore = stats.FairnessScore;
            scenario.ConflictCount = scenario.Assignments.Count(a => a.HasConflict);
            scenario.ModifiedAt = DateTime.Now;

            collection.Update(scenario);
            Log.Information("Scenario aktualisiert: {Name}", scenario.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Aktualisieren des Scenarios");
            throw;
        }
    }

    /// <summary>
    /// Delete scenario
    /// </summary>
    public void DeleteScenario(ObjectId id)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningScenario>("scenarios");
            collection.Delete(id);
            Log.Information("Scenario gelöscht: {Id}", id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Löschen des Scenarios");
            throw;
        }
    }

    /// <summary>
    /// Get all scenarios
    /// </summary>
    public List<PlanningScenario> GetAllScenarios()
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningScenario>("scenarios");
            return collection.FindAll()
                .OrderByDescending(s => s.ModifiedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden der Scenarios");
            return new List<PlanningScenario>();
        }
    }

    /// <summary>
    /// Get scenarios for a specific month
    /// </summary>
    public List<PlanningScenario> GetScenariosForMonth(DateTime month)
    {
        try
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningScenario>("scenarios");
            return collection
                .Find(s => s.Month >= monthStart && s.Month <= monthEnd)
                .OrderByDescending(s => s.ModifiedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden der Scenarios für Monat");
            return new List<PlanningScenario>();
        }
    }

    /// <summary>
    /// Get scenario by ID
    /// </summary>
    public PlanningScenario? GetScenarioById(ObjectId id)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningScenario>("scenarios");
            return collection.FindById(id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden des Scenarios");
            return null;
        }
    }

    /// <summary>
    /// Set scenario as baseline (unset all others for that month)
    /// </summary>
    public void SetAsBaseline(ObjectId id)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningScenario>("scenarios");

            var scenario = collection.FindById(id);
            if (scenario == null)
                throw new InvalidOperationException("Scenario nicht gefunden");

            // Unset all baselines for this month
            var monthScenarios = GetScenariosForMonth(scenario.Month);
            foreach (var s in monthScenarios.Where(s => s.IsBaseline))
            {
                s.IsBaseline = false;
                collection.Update(s);
            }

            // Set new baseline
            scenario.IsBaseline = true;
            collection.Update(scenario);

            Log.Information("Scenario {Name} als Baseline gesetzt", scenario.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Setzen des Baselines");
            throw;
        }
    }

    /// <summary>
    /// Compare two scenarios and return differences
    /// </summary>
    public ScenarioComparison CompareScenarios(PlanningScenario scenario1, PlanningScenario scenario2)
    {
        var comparison = new ScenarioComparison
        {
            Scenario1Name = scenario1.Name,
            Scenario2Name = scenario2.Name,
            Scenario1FairnessScore = scenario1.FairnessScore,
            Scenario2FairnessScore = scenario2.FairnessScore,
            Scenario1Conflicts = scenario1.ConflictCount,
            Scenario2Conflicts = scenario2.ConflictCount
        };

        // Compare assignment counts per person
        var s1Counts = scenario1.Assignments
            .GroupBy(a => a.RessourceName)
            .ToDictionary(g => g.Key, g => g.Count());

        var s2Counts = scenario2.Assignments
            .GroupBy(a => a.RessourceName)
            .ToDictionary(g => g.Key, g => g.Count());

        var allPersons = s1Counts.Keys.Union(s2Counts.Keys).ToList();

        foreach (var person in allPersons)
        {
            var count1 = s1Counts.GetValueOrDefault(person, 0);
            var count2 = s2Counts.GetValueOrDefault(person, 0);

            if (count1 != count2)
            {
                comparison.Differences.Add($"{person}: {count1} → {count2} ({count2 - count1:+0;-0})");
            }
        }

        return comparison;
    }

    /// <summary>
    /// Duplicate scenario (create a copy)
    /// </summary>
    public PlanningScenario DuplicateScenario(ObjectId id, string newName)
    {
        try
        {
            var original = GetScenarioById(id);
            if (original == null)
                throw new InvalidOperationException("Scenario nicht gefunden");

            var duplicate = new PlanningScenario
            {
                Name = newName,
                Description = original.Description,
                Month = original.Month,
                Typ = original.Typ,
                Assignments = new List<PlanningAssignment>(original.Assignments),
                IsBaseline = false
            };

            SaveScenario(duplicate);

            Log.Information("Scenario dupliziert: {Original} -> {New}", original.Name, newName);
            return duplicate;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Duplizieren des Scenarios");
            throw;
        }
    }
}

/// <summary>
/// Result of comparing two scenarios
/// </summary>
public class ScenarioComparison
{
    public string Scenario1Name { get; set; } = string.Empty;
    public string Scenario2Name { get; set; } = string.Empty;
    public double Scenario1FairnessScore { get; set; }
    public double Scenario2FairnessScore { get; set; }
    public int Scenario1Conflicts { get; set; }
    public int Scenario2Conflicts { get; set; }
    public List<string> Differences { get; set; } = new();

    public string Summary =>
        $"Fairness: {Scenario1FairnessScore:F0}% vs {Scenario2FairnessScore:F0}%\n" +
        $"Konflikte: {Scenario1Conflicts} vs {Scenario2Conflicts}\n" +
        $"Unterschiede: {Differences.Count}";
}
