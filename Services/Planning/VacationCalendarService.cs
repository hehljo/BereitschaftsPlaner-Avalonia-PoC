using System;
using System.Collections.Generic;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Data;
using LiteDB;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.Services.Planning;

/// <summary>
/// Service for managing vacation and unavailability days
/// </summary>
public class VacationCalendarService
{
    private readonly string _dbPath;

    public VacationCalendarService(DatabaseService dbService)
    {
        _dbPath = dbService.DatabasePath;

        // Ensure indexes on first run
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<VacationDay>("vacationDays");
        collection.EnsureIndex(x => x.RessourceName);
        collection.EnsureIndex(x => x.Date);
    }

    /// <summary>
    /// Add vacation day for a resource
    /// </summary>
    public void AddVacationDay(VacationDay vacationDay)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");

            // Check if already exists
            var existing = collection.FindOne(x =>
                x.RessourceName == vacationDay.RessourceName &&
                x.Date.Date == vacationDay.Date.Date);

            if (existing != null)
            {
                // Update existing
                existing.Type = vacationDay.Type;
                existing.Note = vacationDay.Note;
                collection.Update(existing);
                Log.Information("Updated vacation day for {Resource} on {Date}",
                    vacationDay.RessourceName, vacationDay.Date.Date);
            }
            else
            {
                // Insert new
                collection.Insert(vacationDay);
                Log.Information("Added vacation day for {Resource} on {Date}",
                    vacationDay.RessourceName, vacationDay.Date.Date);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add vacation day");
            throw;
        }
    }

    /// <summary>
    /// Add multiple vacation days (date range)
    /// </summary>
    public void AddVacationRange(string ressourceName, DateTime startDate, DateTime endDate, VacationType type, string? note = null)
    {
        try
        {
            var currentDate = startDate.Date;
            var end = endDate.Date;

            while (currentDate <= end)
            {
                AddVacationDay(new VacationDay
                {
                    RessourceName = ressourceName,
                    Date = currentDate,
                    Type = type,
                    Note = note
                });

                currentDate = currentDate.AddDays(1);
            }

            Log.Information("Added vacation range for {Resource}: {Start} - {End}",
                ressourceName, startDate.Date, endDate.Date);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add vacation range");
            throw;
        }
    }

    /// <summary>
    /// Remove vacation day
    /// </summary>
    public void RemoveVacationDay(ObjectId id)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");
            collection.Delete(id);
            Log.Information("Removed vacation day: {Id}", id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to remove vacation day");
            throw;
        }
    }

    /// <summary>
    /// Remove all vacation days for a resource on a specific date
    /// </summary>
    public void RemoveVacationDay(string ressourceName, DateTime date)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");
            var deleted = collection.DeleteMany(x =>
                x.RessourceName == ressourceName &&
                x.Date.Date == date.Date);

            Log.Information("Removed {Count} vacation day(s) for {Resource} on {Date}",
                deleted, ressourceName, date.Date);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to remove vacation day");
            throw;
        }
    }

    /// <summary>
    /// Get all vacation days for a resource
    /// </summary>
    public List<VacationDay> GetVacationDays(string ressourceName)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");
            return collection
                .Find(x => x.RessourceName == ressourceName)
                .OrderBy(x => x.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get vacation days for {Resource}", ressourceName);
            return new List<VacationDay>();
        }
    }

    /// <summary>
    /// Get all vacation days in a date range
    /// </summary>
    public List<VacationDay> GetVacationDaysInRange(DateTime start, DateTime end)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");
            return collection
                .Find(x => x.Date.Date >= start.Date && x.Date.Date <= end.Date)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.RessourceName)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get vacation days in range");
            return new List<VacationDay>();
        }
    }

    /// <summary>
    /// Get vacation days as dictionary (resource -> dates)
    /// </summary>
    public Dictionary<string, List<DateTime>> GetVacationDictionary(DateTime? start = null, DateTime? end = null)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");
            IEnumerable<VacationDay> query = collection.FindAll();

            if (start.HasValue && end.HasValue)
            {
                query = query.Where(x => x.Date.Date >= start.Value.Date && x.Date.Date <= end.Value.Date);
            }

            return query
                .GroupBy(x => x.RessourceName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Date.Date).ToList()
                );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get vacation dictionary");
            return new Dictionary<string, List<DateTime>>();
        }
    }

    /// <summary>
    /// Check if resource is available on a specific date
    /// </summary>
    public bool IsResourceAvailable(string ressourceName, DateTime date)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");
            var hasVacation = collection.Exists(x =>
                x.RessourceName == ressourceName &&
                x.Date.Date == date.Date);

            return !hasVacation;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check resource availability");
            return true; // Default to available if error
        }
    }

    /// <summary>
    /// Get count of vacation days per resource in a month
    /// </summary>
    public Dictionary<string, int> GetVacationCountsByMonth(int year, int month)
    {
        try
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            var vacations = GetVacationDaysInRange(start, end);

            return vacations
                .GroupBy(x => x.RessourceName)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get vacation counts");
            return new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// Clear all vacation days (use with caution!)
    /// </summary>
    public void ClearAllVacationDays()
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");
            var count = collection.DeleteAll();
            Log.Warning("Cleared all vacation days: {Count} deleted", count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear vacation days");
            throw;
        }
    }

    /// <summary>
    /// Delete vacation days for a specific resource
    /// </summary>
    public void ClearVacationDaysForResource(string ressourceName)
    {
        try
        {
            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<VacationDay>("vacationDays");
            var count = collection.DeleteMany(x => x.RessourceName == ressourceName);
            Log.Information("Cleared vacation days for {Resource}: {Count} deleted", ressourceName, count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear vacation days for resource");
            throw;
        }
    }
}
