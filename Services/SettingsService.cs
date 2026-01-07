using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using LiteDB;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for LiteDB database operations
/// Manages Ressourcen and BereitschaftsGruppen
/// </summary>
public class DatabaseService
{
    private readonly string _databasePath;
    private const string RESSOURCEN_COLLECTION = "ressourcen";
    private const string GRUPPEN_COLLECTION = "bereitschaftsgruppen";

    public DatabaseService()
    {
        // Find or create database directory
        var dbDir = FindDatabaseDirectory();
        _databasePath = Path.Combine(dbDir, "bereitschaftsplaner.db");

        // Ensure directory exists
        Directory.CreateDirectory(dbDir);

        // Initialize collections
        InitializeDatabase();
    }

    /// <summary>
    /// Initializes database and creates indexes
    /// </summary>
    private void InitializeDatabase()
    {
        using var db = new LiteDatabase(_databasePath);
        
        var ressourcen = db.GetCollection<Ressource>(RESSOURCEN_COLLECTION);
        ressourcen.EnsureIndex(x => x.Name);
        ressourcen.EnsureIndex(x => x.Bezirk);

        var gruppen = db.GetCollection<BereitschaftsGruppe>(GRUPPEN_COLLECTION);
        gruppen.EnsureIndex(x => x.Name);
        gruppen.EnsureIndex(x => x.Bezirk);
    }

    // ============================================================================
    // RESSOURCEN
    // ============================================================================

    /// <summary>
    /// Gets all Ressourcen from database
    /// </summary>
    public List<Ressource> GetRessourcen()
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<Ressource>(RESSOURCEN_COLLECTION);
        return collection.FindAll().OrderBy(r => r.Name).ToList();
    }

    /// <summary>
    /// Saves multiple Ressourcen to database (replaces existing)
    /// </summary>
    public void SaveRessourcen(List<Ressource> ressourcen)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<Ressource>(RESSOURCEN_COLLECTION);
        
        // Clear existing
        collection.DeleteAll();
        
        // Insert new
        collection.InsertBulk(ressourcen);
    }

    /// <summary>
    /// Adds or updates a single Ressource
    /// </summary>
    public void UpsertRessource(Ressource ressource)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<Ressource>(RESSOURCEN_COLLECTION);
        collection.Upsert(ressource);
    }

    /// <summary>
    /// Deletes a Ressource by ID
    /// </summary>
    public bool DeleteRessource(int id)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<Ressource>(RESSOURCEN_COLLECTION);
        return collection.Delete(id);
    }

    /// <summary>
    /// Gets unique Bezirke from Ressourcen
    /// </summary>
    public List<string> GetRessourcenBezirke()
    {
        var ressourcen = GetRessourcen();
        return ressourcen
            .Select(r => r.Bezirk)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct()
            .OrderBy(b => b)
            .ToList();
    }

    // ============================================================================
    // BEREITSCHAFTSGRUPPEN
    // ============================================================================

    /// <summary>
    /// Gets all BereitschaftsGruppen from database
    /// </summary>
    public List<BereitschaftsGruppe> GetBereitschaftsGruppen()
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<BereitschaftsGruppe>(GRUPPEN_COLLECTION);
        return collection.FindAll().OrderBy(g => g.Name).ToList();
    }

    /// <summary>
    /// Saves multiple BereitschaftsGruppen to database (replaces existing)
    /// </summary>
    public void SaveBereitschaftsGruppen(List<BereitschaftsGruppe> gruppen)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<BereitschaftsGruppe>(GRUPPEN_COLLECTION);
        
        // Clear existing
        collection.DeleteAll();
        
        // Insert new
        collection.InsertBulk(gruppen);
    }

    /// <summary>
    /// Adds or updates a single BereitschaftsGruppe
    /// </summary>
    public void UpsertBereitschaftsGruppe(BereitschaftsGruppe gruppe)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<BereitschaftsGruppe>(GRUPPEN_COLLECTION);
        collection.Upsert(gruppe);
    }

    /// <summary>
    /// Deletes a BereitschaftsGruppe by ID
    /// </summary>
    public bool DeleteBereitschaftsGruppe(int id)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<BereitschaftsGruppe>(GRUPPEN_COLLECTION);
        return collection.Delete(id);
    }

    /// <summary>
    /// Gets unique Bezirke from BereitschaftsGruppen
    /// </summary>
    public List<string> GetGruppenBezirke()
    {
        var gruppen = GetBereitschaftsGruppen();
        return gruppen
            .Select(g => g.Bezirk)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct()
            .OrderBy(b => b)
            .ToList();
    }

    /// <summary>
    /// Gets all unique Bezirke from both Ressourcen and Gruppen
    /// </summary>
    public List<string> GetAllBezirke()
    {
        var ressourcenBezirke = GetRessourcenBezirke();
        var gruppenBezirke = GetGruppenBezirke();
        
        return ressourcenBezirke
            .Union(gruppenBezirke)
            .Distinct()
            .OrderBy(b => b)
            .ToList();
    }

    // ============================================================================
    // UTILITY
    // ============================================================================

    /// <summary>
    /// Gets database file path
    /// </summary>
    public string GetDatabasePath()
    {
        return _databasePath;
    }

    /// <summary>
    /// Checks if database has any data
    /// </summary>
    public bool HasData()
    {
        return GetRessourcen().Count > 0 || GetBereitschaftsGruppen().Count > 0;
    }

    /// <summary>
    /// Clears all data from database
    /// </summary>
    public void ClearAllData()
    {
        using var db = new LiteDatabase(_databasePath);
        
        var ressourcen = db.GetCollection<Ressource>(RESSOURCEN_COLLECTION);
        ressourcen.DeleteAll();
        
        var gruppen = db.GetCollection<BereitschaftsGruppe>(GRUPPEN_COLLECTION);
        gruppen.DeleteAll();
    }

    /// <summary>
    /// Finds the database directory in various possible locations
    /// </summary>
    private string FindDatabaseDirectory()
    {
        var possiblePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "data"),
            Path.Combine(Environment.CurrentDirectory, "data"),
            Path.Combine("/root/BereitschaftsPlaner-Avalonia-PoC", "data"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BereitschaftsPlaner", "data")
        };

        // Try to find existing data directory
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        // Use first path as default (create it)
        return Path.GetFullPath(possiblePaths[0]);
    }
}
