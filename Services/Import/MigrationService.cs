using System.Text.Json;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Data;

namespace BereitschaftsPlaner.Avalonia.Services.Import;

/// <summary>
/// Service for migrating data from PowerShell JSON files to LiteDB
/// </summary>
public class MigrationService
{
    private readonly DatabaseService _dbService;
    private readonly string _configPath;

    public MigrationService(DatabaseService dbService)
    {
        _dbService = dbService;

        // Look for PowerShell config folder
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configPath = Path.Combine(appData, "BereitschaftsPlaner", "config");
    }

    /// <summary>
    /// Check if PowerShell JSON files exist for migration
    /// </summary>
    public bool HasPowerShellDataToMigrate()
    {
        if (!Directory.Exists(_configPath))
        {
            return false;
        }

        var ressourcenPath = Path.Combine(_configPath, "ressourcen.json");
        var gruppenPath = Path.Combine(_configPath, "bereitschaftsgruppen.json");

        return File.Exists(ressourcenPath) || File.Exists(gruppenPath);
    }

    /// <summary>
    /// Migrate all PowerShell JSON data to LiteDB
    /// </summary>
    public async Task<MigrationResult> MigrateFromPowerShellJsonAsync()
    {
        var result = new MigrationResult();

        try
        {
            // Migrate Ressourcen
            var ressourcenPath = Path.Combine(_configPath, "ressourcen.json");
            if (File.Exists(ressourcenPath))
            {
                var ressourcenResult = await MigrateRessourcenAsync(ressourcenPath);
                result.RessourcenMigrated = ressourcenResult.Count;
                result.Messages.Add($"✓ {ressourcenResult.Count} Ressourcen migriert");

                if (ressourcenResult.Success)
                {
                    // Backup original JSON
                    var backupPath = ressourcenPath + ".migrated.bak";
                    File.Copy(ressourcenPath, backupPath, overwrite: true);
                    result.Messages.Add($"✓ Backup erstellt: {Path.GetFileName(backupPath)}");
                }
            }

            // Migrate Bereitschaftsgruppen
            var gruppenPath = Path.Combine(_configPath, "bereitschaftsgruppen.json");
            if (File.Exists(gruppenPath))
            {
                var gruppenResult = await MigrateBereitschaftsGruppenAsync(gruppenPath);
                result.GruppenMigrated = gruppenResult.Count;
                result.Messages.Add($"✓ {gruppenResult.Count} Bereitschaftsgruppen migriert");

                if (gruppenResult.Success)
                {
                    // Backup original JSON
                    var backupPath = gruppenPath + ".migrated.bak";
                    File.Copy(gruppenPath, backupPath, overwrite: true);
                    result.Messages.Add($"✓ Backup erstellt: {Path.GetFileName(backupPath)}");
                }
            }

            result.Success = true;
            result.Messages.Add("✓ Migration erfolgreich abgeschlossen");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Messages.Add($"✗ Fehler bei Migration: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Migrate Ressourcen from JSON to LiteDB
    /// </summary>
    private async Task<(bool Success, int Count)> MigrateRessourcenAsync(string jsonPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            var ressourcen = JsonSerializer.Deserialize<List<Ressource>>(json);

            if (ressourcen != null && ressourcen.Count > 0)
            {
                _dbService.SaveRessourcen(ressourcen);
                return (true, ressourcen.Count);
            }

            return (false, 0);
        }
        catch (Exception)
        {
            return (false, 0);
        }
    }

    /// <summary>
    /// Migrate Bereitschaftsgruppen from JSON to LiteDB
    /// </summary>
    private async Task<(bool Success, int Count)> MigrateBereitschaftsGruppenAsync(string jsonPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            var gruppen = JsonSerializer.Deserialize<List<BereitschaftsGruppe>>(json);

            if (gruppen != null && gruppen.Count > 0)
            {
                _dbService.SaveBereitschaftsGruppen(gruppen);
                return (true, gruppen.Count);
            }

            return (false, 0);
        }
        catch (Exception)
        {
            return (false, 0);
        }
    }

    /// <summary>
    /// Export current LiteDB data to JSON (for backup or debugging)
    /// </summary>
    public async Task<bool> ExportToJsonAsync(string exportPath)
    {
        try
        {
            Directory.CreateDirectory(exportPath);

            // Export Ressourcen
            var ressourcen = _dbService.GetAllRessourcen();
            var ressourcenJson = JsonSerializer.Serialize(ressourcen, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(
                Path.Combine(exportPath, "ressourcen.json"),
                ressourcenJson
            );

            // Export Bereitschaftsgruppen
            var gruppen = _dbService.GetAllBereitschaftsGruppen();
            var gruppenJson = JsonSerializer.Serialize(gruppen, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(
                Path.Combine(exportPath, "bereitschaftsgruppen.json"),
                gruppenJson
            );

            // Export Zeitprofile
            var zeitprofile = _dbService.GetAllZeitprofile();
            var zeitprofileJson = JsonSerializer.Serialize(zeitprofile, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(
                Path.Combine(exportPath, "zeitprofile.json"),
                zeitprofileJson
            );

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// Result of migration operation
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public int RessourcenMigrated { get; set; }
    public int GruppenMigrated { get; set; }
    public List<string> Messages { get; set; } = new();

    public string GetSummary()
    {
        return string.Join("\n", Messages);
    }
}
