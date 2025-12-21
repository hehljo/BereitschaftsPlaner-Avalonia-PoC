using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using BereitschaftsPlaner.Avalonia.Models;

namespace BereitschaftsPlaner.Avalonia.Services.Data;

/// <summary>
/// Service for database operations using LiteDB
/// Provides CRUD operations for all entities
/// </summary>
public class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService()
    {
        // Platform-specific AppData path
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "BereitschaftsPlaner");
        Directory.CreateDirectory(appFolder);
        _dbPath = Path.Combine(appFolder, "bereitschaftsplaner.db");
    }

    public string DatabasePath => _dbPath;

    #region Ressourcen

    public List<Ressource> GetAllRessourcen()
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Ressource>("ressourcen");
        return collection.FindAll().ToList();
    }

    public Ressource? GetRessourceById(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Ressource>("ressourcen");
        return collection.FindById(id);
    }

    public int InsertRessource(Ressource ressource)
    {
        ressource.CreatedAt = DateTime.Now;
        ressource.UpdatedAt = DateTime.Now;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Ressource>("ressourcen");
        return collection.Insert(ressource);
    }

    public bool UpdateRessource(Ressource ressource)
    {
        ressource.UpdatedAt = DateTime.Now;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Ressource>("ressourcen");
        return collection.Update(ressource);
    }

    public bool DeleteRessource(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Ressource>("ressourcen");
        return collection.Delete(id);
    }

    public void SaveRessourcen(List<Ressource> ressourcen)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Ressource>("ressourcen");
        collection.DeleteAll();

        foreach (var ressource in ressourcen)
        {
            ressource.CreatedAt = DateTime.Now;
            ressource.UpdatedAt = DateTime.Now;
        }

        collection.InsertBulk(ressourcen);
    }

    #endregion

    #region Bereitschaftsgruppen

    public List<BereitschaftsGruppe> GetAllBereitschaftsGruppen()
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<BereitschaftsGruppe>("bereitschaftsgruppen");
        return collection.FindAll().ToList();
    }

    public BereitschaftsGruppe? GetBereitschaftsGruppeById(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<BereitschaftsGruppe>("bereitschaftsgruppen");
        return collection.FindById(id);
    }

    public int InsertBereitschaftsGruppe(BereitschaftsGruppe gruppe)
    {
        gruppe.CreatedAt = DateTime.Now;
        gruppe.UpdatedAt = DateTime.Now;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<BereitschaftsGruppe>("bereitschaftsgruppen");
        return collection.Insert(gruppe);
    }

    public bool UpdateBereitschaftsGruppe(BereitschaftsGruppe gruppe)
    {
        gruppe.UpdatedAt = DateTime.Now;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<BereitschaftsGruppe>("bereitschaftsgruppen");
        return collection.Update(gruppe);
    }

    public bool DeleteBereitschaftsGruppe(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<BereitschaftsGruppe>("bereitschaftsgruppen");
        return collection.Delete(id);
    }

    public void SaveBereitschaftsGruppen(List<BereitschaftsGruppe> gruppen)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<BereitschaftsGruppe>("bereitschaftsgruppen");
        collection.DeleteAll();

        foreach (var gruppe in gruppen)
        {
            gruppe.CreatedAt = DateTime.Now;
            gruppe.UpdatedAt = DateTime.Now;
        }

        collection.InsertBulk(gruppen);
    }

    #endregion

    #region Zeitprofile

    public List<Zeitprofil> GetAllZeitprofile()
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Zeitprofil>("zeitprofile");
        return collection.FindAll().ToList();
    }

    public Zeitprofil? GetZeitprofilById(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Zeitprofil>("zeitprofile");
        return collection.FindById(id);
    }

    public int InsertZeitprofil(Zeitprofil profil)
    {
        profil.CreatedAt = DateTime.Now;
        profil.UpdatedAt = DateTime.Now;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Zeitprofil>("zeitprofile");
        return collection.Insert(profil);
    }

    public bool UpdateZeitprofil(Zeitprofil profil)
    {
        profil.UpdatedAt = DateTime.Now;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Zeitprofil>("zeitprofile");
        return collection.Update(profil);
    }

    public bool DeleteZeitprofil(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Zeitprofil>("zeitprofile");
        return collection.Delete(id);
    }

    public void InitializeDefaultZeitprofile()
    {
        var existing = GetAllZeitprofile();
        if (existing.Count > 0) return; // Already initialized

        var bdProfil = new Zeitprofil
        {
            Name = "Bereitschaftsdienst",
            StartZeit = "16:00",
            EndZeit = "07:30",
            Folgetag = true,
            Montag = true,
            Dienstag = true,
            Mittwoch = true,
            Donnerstag = true,
            Freitag = true,
            Samstag = true,
            Sonntag = true
        };

        var tdProfil = new Zeitprofil
        {
            Name = "Tagdienst",
            StartZeit = "07:30",
            EndZeit = "16:00",
            Folgetag = false,
            Montag = true,
            Dienstag = true,
            Mittwoch = true,
            Donnerstag = true,
            Freitag = true,
            Samstag = false,
            Sonntag = false
        };

        InsertZeitprofil(bdProfil);
        InsertZeitprofil(tdProfil);
    }

    #endregion

    #region Bereitschaften

    public List<Bereitschaft> GetAllBereitschaften()
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Bereitschaft>("bereitschaften");
        return collection.FindAll().ToList();
    }

    public Bereitschaft? GetBereitschaftById(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Bereitschaft>("bereitschaften");
        return collection.FindById(id);
    }

    public int InsertBereitschaft(Bereitschaft bereitschaft)
    {
        bereitschaft.CreatedAt = DateTime.Now;
        bereitschaft.UpdatedAt = DateTime.Now;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Bereitschaft>("bereitschaften");
        return collection.Insert(bereitschaft);
    }

    public bool UpdateBereitschaft(Bereitschaft bereitschaft)
    {
        bereitschaft.UpdatedAt = DateTime.Now;

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Bereitschaft>("bereitschaften");
        return collection.Update(bereitschaft);
    }

    public bool DeleteBereitschaft(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Bereitschaft>("bereitschaften");
        return collection.Delete(id);
    }

    public void SaveBereitschaften(List<Bereitschaft> bereitschaften)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Bereitschaft>("bereitschaften");
        collection.DeleteAll();

        foreach (var bereitschaft in bereitschaften)
        {
            bereitschaft.CreatedAt = DateTime.Now;
            bereitschaft.UpdatedAt = DateTime.Now;
        }

        collection.InsertBulk(bereitschaften);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Check if database exists and has been initialized
    /// </summary>
    public bool IsDatabaseInitialized()
    {
        return File.Exists(_dbPath);
    }

    /// <summary>
    /// Get database file size in MB
    /// </summary>
    public double GetDatabaseSizeMB()
    {
        if (!File.Exists(_dbPath)) return 0;
        var fileInfo = new FileInfo(_dbPath);
        return fileInfo.Length / (1024.0 * 1024.0);
    }

    /// <summary>
    /// Compact/shrink the database file
    /// </summary>
    public void CompactDatabase()
    {
        if (!File.Exists(_dbPath)) return;

        using var db = new LiteDatabase(_dbPath);
        db.Rebuild();
    }

    /// <summary>
    /// Clear all data from database (Ressourcen, Bereitschaftsgruppen, Zeitprofile)
    /// </summary>
    public void ClearAllData()
    {
        using var db = new LiteDatabase(_dbPath);

        // Clear all collections
        db.GetCollection<Ressource>("ressourcen").DeleteAll();
        db.GetCollection<BereitschaftsGruppe>("bereitschaftsgruppen").DeleteAll();
        db.GetCollection<Zeitprofil>("zeitprofile").DeleteAll();

        // Compact after delete
        db.Rebuild();
    }

    #endregion
}
