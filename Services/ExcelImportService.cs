using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using BereitschaftsPlaner.Avalonia.Models;
using ExcelDataReader;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for importing Excel files and converting to JSON
/// Cross-platform alternative to Excel COM (uses ExcelDataReader)
/// </summary>
public class ExcelImportService
{
    static ExcelImportService()
    {
        // Required for ExcelDataReader to work with different encodings
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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

            // Find columns (flexible matching like PowerShell version)
            int colName = FindColumn(table, "Ressourcenname", "Name", "Ressource");
            int colBezirk = FindColumn(table, "Bezirk", "Organisationsdaten", "District");

            if (colName == -1 || colBezirk == -1)
            {
                var availableColumns = string.Join(", ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                return (false, new List<Ressource>(),
                    $"Erforderliche Spalten nicht gefunden.\n\nBenötigt: 'Ressourcenname', 'Bezirk'\nGefunden: {availableColumns}");
            }

            // Read data rows
            foreach (DataRow row in table.Rows)
            {
                var name = row[colName]?.ToString()?.Trim() ?? string.Empty;
                var bezirk = row[colBezirk]?.ToString()?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    ressourcen.Add(new Ressource
                    {
                        Name = name,
                        Bezirk = bezirk
                    });
                }
            }

            return (true, ressourcen, $"{ressourcen.Count} Ressourcen erfolgreich gelesen");
        }
        catch (Exception ex)
        {
            return (false, new List<Ressource>(), $"Fehler beim Lesen: {ex.Message}");
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
    /// Finds column index by flexible name matching
    /// </summary>
    private int FindColumn(DataTable table, params string[] possibleNames)
    {
        for (int i = 0; i < table.Columns.Count; i++)
        {
            var columnName = table.Columns[i].ColumnName;

            foreach (var possibleName in possibleNames)
            {
                if (columnName.Contains(possibleName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
