using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using BereitschaftsPlaner.Avalonia.Models;
using ExcelDataReader;
using DataSettingsService = BereitschaftsPlaner.Avalonia.Services.Data.SettingsService;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for importing Excel files and converting to JSON
/// Cross-platform alternative to Excel COM (uses ExcelDataReader)
/// </summary>
public class ExcelImportService
{
    private readonly DataSettingsService _settingsService;

    static ExcelImportService()
    {
        // Required for ExcelDataReader to work with different encodings
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ExcelImportService(DataSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Imports resources from Excel file
    /// </summary>
    /// <param name="filePath">Path to Excel file</param>
    /// <returns>Tuple with success status, list of resources, and message</returns>
    public (bool Success, List<Ressource> Ressourcen, string Message) ImportRessourcen(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return (false, new List<Ressource>(), $"Datei nicht gefunden: {filePath}");
            }

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true // First row is header
                }
            });

            if (dataSet.Tables.Count == 0)
            {
                return (false, new List<Ressource>(), "Keine Tabellen in Excel gefunden");
            }

            var table = dataSet.Tables[0];
            var ressourcen = new List<Ressource>();

            // Log all available columns
            var availableColumns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            Serilog.Log.Debug($"ExcelImportService.ImportRessourcen: Excel has {availableColumns.Count} columns");
            for (int i = 0; i < availableColumns.Count; i++)
            {
                Serilog.Log.Debug($"  Column {i}: '{availableColumns[i]}'");
            }

            // Find columns using settings
            var settings = _settingsService.GetSettings().ExcelImport;
            int colName = FindColumn(table, settings.RessourceName);
            int colBezirk = FindColumn(table, settings.RessourceBezirk);

            // Log which columns were found
            Serilog.Log.Debug($"ExcelImportService.ImportRessourcen: colName={colName}, colBezirk={colBezirk}");
            if (colName != -1)
            {
                Serilog.Log.Debug($"  Name column: '{table.Columns[colName].ColumnName}'");
            }
            if (colBezirk != -1)
            {
                Serilog.Log.Debug($"  Bezirk column: '{table.Columns[colBezirk].ColumnName}'");
            }

            if (colName == -1 || colBezirk == -1)
            {
                var availableColumnsStr = string.Join(", ", availableColumns);
                return (false, new List<Ressource>(),
                    $"Erforderliche Spalten nicht gefunden.\n\nBenötigt: 'Ressourcenname', 'Bezirk'\nGefunden: {availableColumnsStr}");
            }

            // Read data rows
            int rowIndex = 0;
            foreach (DataRow row in table.Rows)
            {
                var name = row[colName]?.ToString()?.Trim() ?? string.Empty;
                var bezirk = row[colBezirk]?.ToString()?.Trim() ?? string.Empty;

                if (rowIndex < 3) // Log first 3 rows for debugging
                {
                    Serilog.Log.Debug($"ExcelImportService.ImportRessourcen: Row {rowIndex} - Name='{name}', Bezirk='{bezirk}'");
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    ressourcen.Add(new Ressource
                    {
                        Name = name,
                        Bezirk = bezirk
                    });
                }
                rowIndex++;
            }

            Serilog.Log.Information($"ExcelImportService.ImportRessourcen: Read {ressourcen.Count} Ressourcen from {rowIndex} rows");
            if (ressourcen.Count > 0)
            {
                Serilog.Log.Debug($"ExcelImportService.ImportRessourcen: First item - Name='{ressourcen[0].Name}', Bezirk='{ressourcen[0].Bezirk}'");
            }

            return (true, ressourcen, $"{ressourcen.Count} Ressourcen erfolgreich gelesen");
        }
        catch (Exception ex)
        {
            return (false, new List<Ressource>(), $"Fehler beim Lesen: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports Bereitschaftsgruppen from Excel file
    /// </summary>
    public (bool Success, List<BereitschaftsGruppe> Gruppen, string Message) ImportBereitschaftsGruppen(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return (false, new List<BereitschaftsGruppe>(), $"Datei nicht gefunden: {filePath}");
            }

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            if (dataSet.Tables.Count == 0)
            {
                return (false, new List<BereitschaftsGruppe>(), "Keine Tabellen in Excel gefunden");
            }

            var table = dataSet.Tables[0];
            var gruppen = new List<BereitschaftsGruppe>();

            // Log all available columns
            var availableColumns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            Serilog.Log.Debug($"ExcelImportService.ImportBereitschaftsGruppen: Excel has {availableColumns.Count} columns");
            for (int i = 0; i < availableColumns.Count; i++)
            {
                Serilog.Log.Debug($"  Column {i}: '{availableColumns[i]}'");
            }

            // Find columns using settings
            var settings = _settingsService.GetSettings().ExcelImport;
            int colName = FindColumn(table, settings.GruppenName);
            int colBezirk = FindColumn(table, settings.GruppenBezirk);
            int colVerantwortlich = FindColumn(table, settings.GruppenVerantwortlich);

            // Log which columns were found
            Serilog.Log.Debug($"ExcelImportService.ImportBereitschaftsGruppen: colName={colName}, colBezirk={colBezirk}, colVerantwortlich={colVerantwortlich}");
            if (colName != -1)
            {
                Serilog.Log.Debug($"  Name column: '{table.Columns[colName].ColumnName}'");
            }
            if (colBezirk != -1)
            {
                Serilog.Log.Debug($"  Bezirk column: '{table.Columns[colBezirk].ColumnName}'");
            }
            if (colVerantwortlich != -1)
            {
                Serilog.Log.Debug($"  Verantwortlich column: '{table.Columns[colVerantwortlich].ColumnName}'");
            }

            if (colName == -1)
            {
                var availableColumnsStr = string.Join(", ", availableColumns);
                return (false, new List<BereitschaftsGruppe>(),
                    $"Spalte 'Name' nicht gefunden.\n\nGefunden: {availableColumnsStr}");
            }

            // Read data rows
            int rowIndex = 0;
            foreach (DataRow row in table.Rows)
            {
                var name = row[colName]?.ToString()?.Trim() ?? string.Empty;
                var bezirk = colBezirk != -1 ? (row[colBezirk]?.ToString()?.Trim() ?? string.Empty) : string.Empty;
                var verantwortlich = colVerantwortlich != -1 ? (row[colVerantwortlich]?.ToString()?.Trim() ?? string.Empty) : string.Empty;

                if (rowIndex < 3) // Log first 3 rows for debugging
                {
                    Serilog.Log.Debug($"ExcelImportService.ImportBereitschaftsGruppen: Row {rowIndex} - Name='{name}', Bezirk='{bezirk}', Verantwortlich='{verantwortlich}'");
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    gruppen.Add(new BereitschaftsGruppe
                    {
                        Name = name,
                        Bezirk = bezirk,
                        VerantwortlichePerson = verantwortlich
                    });
                }
                rowIndex++;
            }

            Serilog.Log.Information($"ExcelImportService.ImportBereitschaftsGruppen: Read {gruppen.Count} Gruppen from {rowIndex} rows");
            if (gruppen.Count > 0)
            {
                Serilog.Log.Debug($"ExcelImportService.ImportBereitschaftsGruppen: First item - Name='{gruppen[0].Name}', Bezirk='{gruppen[0].Bezirk}'");
            }

            return (true, gruppen, $"{gruppen.Count} Bereitschaftsgruppen erfolgreich gelesen");
        }
        catch (Exception ex)
        {
            return (false, new List<BereitschaftsGruppe>(), $"Fehler beim Lesen: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves resources to JSON file
    /// </summary>
    public (bool Success, string Message) SaveToJson(List<Ressource> ressourcen, string jsonPath)
    {
        try
        {
            // Create backup if file exists
            if (File.Exists(jsonPath))
            {
                var backupPath = $"{jsonPath}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                File.Copy(jsonPath, backupPath, true);
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(ressourcen, options);
            File.WriteAllText(jsonPath, json, Encoding.UTF8);

            return (true, $"JSON gespeichert: {jsonPath}");
        }
        catch (Exception ex)
        {
            return (false, $"Fehler beim Speichern: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds column using flexible mapping configuration
    /// IMPORTANT: Skips first N columns (D365 metadata) based on settings
    /// </summary>
    private int FindColumn(DataTable table, ColumnMapping mapping)
    {
        var settings = _settingsService.GetSettings().ExcelImport;
        var skipColumns = settings.SkipFirstColumns;
        var searchTerm = mapping.SearchTerm;
        var matchType = mapping.MatchType;

        for (int i = skipColumns; i < table.Columns.Count; i++)
        {
            var columnName = table.Columns[i].ColumnName;
            bool isMatch = matchType switch
            {
                Models.MatchType.Contains => columnName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase),
                Models.MatchType.StartsWith => columnName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase),
                Models.MatchType.Exact => columnName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase),
                _ => false
            };

            if (isMatch)
            {
                Serilog.Log.Debug($"FindColumn: Found '{searchTerm}' (MatchType={matchType}) in column {i} ('{columnName}')");
                return i;
            }
        }

        Serilog.Log.Warning($"FindColumn: No match found for '{searchTerm}' (MatchType={matchType})");
        return -1;
    }
}
