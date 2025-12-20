using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for Excel operations specific to Bereitschaftsdienst generation and editing
/// </summary>
public class BereitschaftsExcelService
{
    // ============================================================================
    // GENERATION
    // ============================================================================

    /// <summary>
    /// Generates a new Excel file with Bereitschaftsdienste based on configuration
    /// </summary>
    /// <param name="outputPath">Path where to save the Excel file</param>
    /// <param name="gruppen">Selected Bereitschaftsgruppen</param>
    /// <param name="ressource">Responsible resource/person</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="zeitprofilService">Service to get Zeitprofile for groups</param>
    /// <param name="progressCallback">Optional progress callback (current, total, message)</param>
    /// <returns>Result with success status and message</returns>
    public ServiceResult GenerateBereitschaften(
        string outputPath,
        List<BereitschaftsGruppe> gruppen,
        Ressource ressource,
        DateTime startDate,
        DateTime endDate,
        ZeitprofilService zeitprofilService,
        Action<int, int, string>? progressCallback = null)
    {
        try
        {
            if (gruppen == null || gruppen.Count == 0)
            {
                return new ServiceResult { Success = false, Message = "Keine Gruppen ausgewählt" };
            }

            if (ressource == null)
            {
                return new ServiceResult { Success = false, Message = "Keine Ressource ausgewählt" };
            }

            if (endDate < startDate)
            {
                return new ServiceResult { Success = false, Message = "Enddatum muss nach Startdatum liegen" };
            }

            // Calculate total entries
            var totalDays = (endDate - startDate).Days + 1;
            var totalEntries = gruppen.Count * totalDays;
            var currentEntry = 0;

            var bereitschaften = new List<BereitschaftEntry>();

            // Generate entries for each group and each day
            foreach (var gruppe in gruppen)
            {
                progressCallback?.Invoke(currentEntry, totalEntries, $"Generiere: {gruppe.Name}");

                // Get Zeitprofil for this group
                var zeitprofile = zeitprofilService.GetAlleZeitprofile();
                var profilID = zeitprofilService.GetProfilIDForGruppe(gruppe.Name);

                Zeitprofil? zeitprofil = null;
                if (!string.IsNullOrEmpty(profilID) && zeitprofile.ContainsKey(profilID))
                {
                    zeitprofil = zeitprofile[profilID];
                }

                // If no Zeitprofil found, use Standard
                if (zeitprofil == null && zeitprofile.ContainsKey("Standard"))
                {
                    zeitprofil = zeitprofile["Standard"];
                }

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    // Determine service type and times for this day
                    var (startZeit, endZeit, typ) = GetDienstForDate(date, zeitprofil);

                    if (typ != null) // Only create entry if service is defined for this day
                    {
                        bereitschaften.Add(new BereitschaftEntry
                        {
                            Id = bereitschaften.Count + 1,
                            Datum = date,
                            GruppeName = gruppe.Name,
                            RessourceName = ressource.Name,
                            StartZeit = startZeit ?? "16:00",
                            EndZeit = endZeit ?? "07:30",
                            Typ = typ
                        });
                    }

                    currentEntry++;
                    progressCallback?.Invoke(currentEntry, totalEntries, $"Generiere: {gruppe.Name} - {date:dd.MM.yyyy}");
                }
            }

            // TODO: Write to actual Excel file using ClosedXML or EPPlus
            // For now, we'll create a mock file to demonstrate the structure

            progressCallback?.Invoke(totalEntries, totalEntries, "Schreibe Excel-Datei...");

            // Mock: Write a simple text file with the data structure
            WriteMockExcel(outputPath, bereitschaften);

            return new ServiceResult
            {
                Success = true,
                Message = $"Erfolgreich {bereitschaften.Count} Einträge generiert und gespeichert",
                Data = bereitschaften
            };
        }
        catch (Exception ex)
        {
            return new ServiceResult
            {
                Success = false,
                Message = $"Fehler bei Generierung: {ex.Message}"
            };
        }
    }

    // ============================================================================
    // IMPORT (EDITING)
    // ============================================================================

    /// <summary>
    /// Imports Bereitschaftsdienste from an existing Excel file for editing
    /// </summary>
    public ServiceResult ImportBereitschaften(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new ServiceResult { Success = false, Message = "Datei nicht gefunden" };
            }

            // TODO: Use ExcelDataReader or ClosedXML to read the file
            // For now, return mock data

            var mockEntries = GenerateMockEntries();

            return new ServiceResult
            {
                Success = true,
                Message = $"{mockEntries.Count} Einträge geladen",
                Data = mockEntries
            };
        }
        catch (Exception ex)
        {
            return new ServiceResult
            {
                Success = false,
                Message = $"Fehler beim Laden: {ex.Message}"
            };
        }
    }

    // ============================================================================
    // EXPORT (SAVE MODIFICATIONS)
    // ============================================================================

    /// <summary>
    /// Saves modified Bereitschaftsdienste back to Excel file
    /// </summary>
    public ServiceResult SaveBereitschaften(string filePath, List<BereitschaftEntry> bereitschaften)
    {
        try
        {
            if (bereitschaften == null || bereitschaften.Count == 0)
            {
                return new ServiceResult { Success = false, Message = "Keine Daten zum Speichern" };
            }

            // TODO: Use ClosedXML or EPPlus to write to existing Excel file
            // Preserve D365 XML metadata in columns A, B, C

            WriteMockExcel(filePath, bereitschaften);

            return new ServiceResult
            {
                Success = true,
                Message = $"{bereitschaften.Count} Einträge gespeichert"
            };
        }
        catch (Exception ex)
        {
            return new ServiceResult
            {
                Success = false,
                Message = $"Fehler beim Speichern: {ex.Message}"
            };
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    /// <summary>
    /// Determines service type and times for a specific date based on Zeitprofil
    /// </summary>
    private (string? StartZeit, string? EndZeit, string? Typ) GetDienstForDate(DateTime date, Zeitprofil? zeitprofil)
    {
        if (zeitprofil == null)
        {
            // Default: Bereitschaftsdienst (BD) every day
            return ("16:00", "07:30", "BD");
        }

        var dayName = date.ToString("dddd", new System.Globalization.CultureInfo("de-DE"));

        // Check if it's a holiday
        // TODO: Integrate FeiertagsManager
        bool isHoliday = false;

        if (isHoliday)
        {
            // Treat as configured (usually Sunday)
            dayName = zeitprofil.Feiertage.BehandelnWie;
        }

        // Check Bereitschaftstage (BD)
        var bereitschaftsTag = zeitprofil.BereitschaftsTage.FirstOrDefault(t => t.Tag == dayName);
        if (bereitschaftsTag != null && !string.IsNullOrEmpty(bereitschaftsTag.Von))
        {
            return (bereitschaftsTag.Von, bereitschaftsTag.Bis, "BD");
        }

        // Check Tagesdienste (TD)
        var tagesdienst = zeitprofil.Tagesdienste.FirstOrDefault(t => t.Tag == dayName);
        if (tagesdienst != null && !string.IsNullOrEmpty(tagesdienst.Von))
        {
            return (tagesdienst.Von, tagesdienst.Bis, "TD");
        }

        // Use default from profile
        if (zeitprofil.StandardTypFuerUndefiniert == "BD")
        {
            return ("16:00", "07:30", "BD");
        }
        else if (zeitprofil.StandardTypFuerUndefiniert == "TD")
        {
            return ("07:30", "16:00", "TD");
        }

        // No service for this day
        return (null, null, null);
    }

    /// <summary>
    /// Generates mock entries for testing
    /// </summary>
    private List<BereitschaftEntry> GenerateMockEntries()
    {
        var entries = new List<BereitschaftEntry>();
        var startDate = DateTime.Now;

        for (int i = 0; i < 20; i++)
        {
            entries.Add(new BereitschaftEntry
            {
                Id = i + 1,
                Datum = startDate.AddDays(i),
                GruppeName = $"EMUW194 {(i % 3 == 0 ? "Augsburg" : "Umland")}",
                RessourceName = $"Max Mustermann {(i % 5) + 1}",
                StartZeit = i % 2 == 0 ? "16:00" : "07:30",
                EndZeit = i % 2 == 0 ? "07:30" : "16:00",
                Typ = i % 2 == 0 ? "BD" : "TD"
            });
        }

        return entries;
    }

    /// <summary>
    /// Writes a mock Excel file (CSV format for now)
    /// TODO: Replace with actual Excel writing using ClosedXML
    /// </summary>
    private void WriteMockExcel(string filePath, List<BereitschaftEntry> bereitschaften)
    {
        // For demonstration, write CSV format
        // In production, this would write proper .xlsx format
        var csvPath = Path.ChangeExtension(filePath, ".csv");

        using var writer = new StreamWriter(csvPath);

        // Header
        writer.WriteLine("ID;Datum;Gruppe;Ressource;Von;Bis;Typ");

        // Data
        foreach (var entry in bereitschaften.OrderBy(e => e.Datum).ThenBy(e => e.GruppeName))
        {
            writer.WriteLine($"{entry.Id};{entry.Datum:dd.MM.yyyy};{entry.GruppeName};{entry.RessourceName};{entry.StartZeit};{entry.EndZeit};{entry.Typ}");
        }

        // Also create a .xlsx file marker
        File.WriteAllText(filePath, $"Excel-Datei würde hier erstellt werden.\nCSV-Version verfügbar unter: {csvPath}\nAnzahl Einträge: {bereitschaften.Count}");
    }
}

/// <summary>
/// Generic service result object
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}
