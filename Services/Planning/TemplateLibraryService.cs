using System;
using System.Collections.Generic;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Data;
using LiteDB;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.Services.Planning;

/// <summary>
/// Service for managing planning templates
/// </summary>
public class TemplateLibraryService
{
    private readonly string _dbPath;

    public TemplateLibraryService(DatabaseService dbService)
    {
        _dbPath = dbService.DatabasePath;

        // Ensure indexes
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<PlanningTemplate>("templates");
        collection.EnsureIndex(x => x.Name);
        collection.EnsureIndex(x => x.Category);
        collection.EnsureIndex(x => x.CreatedAt);
    }

    /// <summary>
    /// Save current planning as template
    /// </summary>
    public void SaveTemplate(PlanningTemplate template)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningTemplate>("templates");

            // Check for duplicate name
            var existing = collection.FindOne(x => x.Name == template.Name);
            if (existing != null)
            {
                throw new InvalidOperationException($"Template mit Name '{template.Name}' existiert bereits!");
            }

            collection.Insert(template);
            Log.Information("Template gespeichert: {Name} ({Count} Assignments)",
                template.Name, template.AssignmentCount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Speichern des Templates");
            throw;
        }
    }

    /// <summary>
    /// Update existing template
    /// </summary>
    public void UpdateTemplate(PlanningTemplate template)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningTemplate>("templates");
            collection.Update(template);
            Log.Information("Template aktualisiert: {Name}", template.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Aktualisieren des Templates");
            throw;
        }
    }

    /// <summary>
    /// Delete template
    /// </summary>
    public void DeleteTemplate(ObjectId id)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningTemplate>("templates");
            var deleted = collection.Delete(id);

            if (deleted)
            {
                Log.Information("Template gelöscht: {Id}", id);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Löschen des Templates");
            throw;
        }
    }

    /// <summary>
    /// Get all templates
    /// </summary>
    public List<PlanningTemplate> GetAllTemplates()
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningTemplate>("templates");
            return collection.FindAll()
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden der Templates");
            return new List<PlanningTemplate>();
        }
    }

    /// <summary>
    /// Get templates by category
    /// </summary>
    public List<PlanningTemplate> GetTemplatesByCategory(string category)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningTemplate>("templates");
            return collection.Find(x => x.Category == category)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden der Templates für Kategorie {Category}", category);
            return new List<PlanningTemplate>();
        }
    }

    /// <summary>
    /// Get single template by ID
    /// </summary>
    public PlanningTemplate? GetTemplateById(ObjectId id)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningTemplate>("templates");
            return collection.FindById(id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden des Templates {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Get all unique categories
    /// </summary>
    public List<string> GetCategories()
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningTemplate>("templates");
            return collection.FindAll()
                .Select(x => x.Category)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden der Kategorien");
            return new List<string> { "Standard" };
        }
    }

    /// <summary>
    /// Apply template to target month
    /// </summary>
    public List<PlanningAssignment> ApplyTemplate(
        PlanningTemplate template,
        DateTime targetMonth,
        List<BereitschaftsGruppe> groups)
    {
        try
        {
            var assignments = new List<PlanningAssignment>();
            var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month);

            foreach (var kvp in template.Assignments)
            {
                var dayOfMonth = kvp.Key;

                // Skip if day doesn't exist in target month (e.g., 31st in February)
                if (dayOfMonth > daysInMonth)
                    continue;

                var data = kvp.Value;
                var date = monthStart.AddDays(dayOfMonth - 1);

                assignments.Add(new PlanningAssignment
                {
                    Date = date,
                    GruppeName = data.GruppeName,
                    RessourceName = data.RessourceName,
                    Typ = template.Typ,
                    StartZeit = data.StartZeit,
                    EndZeit = data.EndZeit
                });
            }

            Log.Information("Template angewendet: {Name} auf {Month:MMMM yyyy} ({Count} Assignments)",
                template.Name, targetMonth, assignments.Count);

            return assignments;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Anwenden des Templates");
            throw;
        }
    }

    /// <summary>
    /// Rename template
    /// </summary>
    public void RenameTemplate(ObjectId id, string newName)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<PlanningTemplate>("templates");

            var template = collection.FindById(id);
            if (template == null)
            {
                throw new InvalidOperationException("Template nicht gefunden!");
            }

            // Check for duplicate name
            var existing = collection.FindOne(x => x.Name == newName && x.Id != id);
            if (existing != null)
            {
                throw new InvalidOperationException($"Template mit Name '{newName}' existiert bereits!");
            }

            template.Name = newName;
            collection.Update(template);

            Log.Information("Template umbenannt: {OldName} -> {NewName}", template.Name, newName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Umbenennen des Templates");
            throw;
        }
    }
}
