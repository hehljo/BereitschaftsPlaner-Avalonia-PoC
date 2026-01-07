using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.ViewModels;
using ClosedXML.Excel;
using System.Threading.Tasks;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for Excel operations specific to Bereitschaftsdienst generation and editing
/// Uses ClosedXML for real Excel file manipulation
/// </summary>
public class BereitschaftsExcelService
{
    private const string TEMPLATE_FILENAME = "template.xlsx";

    // ============================================================================
    // GENERATION
    // ============================================================================

    /// <summary>
    /// Generates a new Excel file with Bereitschaftsdienste based on configuration
    /// </summary>
    public ServiceResult GenerateBereitschaften(
        string outputPath,
        List<BereitschaftsGruppe> gruppen,
        Ressource ressource,
        DateTime startDate,
        DateTime endDate,
        ZeitprofilService zeitprofilService,
        FeiertagsService feiertagsService,
        Action<int, int, string>? progressCallback = null)
    {
        try
        {
            // Validation
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

            // Find template file
            var templatePath = FindTemplatePath();
            if (templatePath == null || !File.Exists(templatePath))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = $"Template-Datei nicht gefunden!\nErwartet: config/{TEMPLATE_FILENAME}\nBitte Template aus D365 exportieren."
                };
            }

            // Load template
            using var workbook = new XLWorkbook(templatePath);
            var worksheet = workbook.Worksheet(1);

            // Find header row (usually row 1)
            var headerRow = 1;

            // Clear any existing data rows (keep header)
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;
            if (lastRow > headerRow)
            {
                worksheet.Rows(headerRow + 1, lastRow).Delete();
            }

            // Calculate total entries
            var totalDays = (endDate - startDate).Days + 1;
            var totalEntries = gruppen.Count * totalDays;
            var currentEntry = 0;
            var currentRow = headerRow + 1;

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
                    var (startZeit, endZeit, typ) = GetDienstForDateAsync(
                        date,
                        zeitprofil,
                        feiertagsService
                    ).GetAwaiter().GetResult(); // Sync call in loop context

                    if (typ != null) // Only create entry if service is defined for this day
                    {
                        var entry = new BereitschaftEntry
                        {
                            Id = bereitschaften.Count + 1,
                            Datum = date,
                            GruppeName = gruppe.Name,
                            RessourceName = ressource.Name,
                            StartZeit = startZeit ?? "16:00",
                            EndZeit = endZeit ?? "07:30",
                            Typ = typ
                        };

                        bereitschaften.Add(entry);

                        // Write to Excel
                        WriteEntryToWorksheet(worksheet, currentRow, entry, date);
                        currentRow++;
                    }

                    currentEntry++;
                    progressCallback?.Invoke(currentEntry, totalEntries, $"Generiere: {gruppe.Name} - {date:dd.MM.yyyy}");
                }
            }

            progressCallback?.Invoke(totalEntries, totalEntries, "Speichere Excel-Datei...");

            // Save workbook
            workbook.SaveAs(outputPath);

            return new ServiceResult
            {
                Success = true,
                Message = $"{bereitschaften.Count} Einträge generiert und gespeichert",
                Data = bereitschaften
            };
        }
        catch (Exception ex)
        {
            return new ServiceResult
            {
                Success = false,
                Message = $"Fehler bei Generierung: {ex.Message}\n{ex.StackTrace}"
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

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);

            var entries = new List<BereitschaftEntry>();

            // Find header row (usually row 1)
            var headerRow = 1;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;

            // Find column indices (case-insensitive)
            var columns = FindColumnIndices(worksheet, headerRow);

            // Read all data rows
            for (int row = headerRow + 1; row <= lastRow; row++)
            {
                try
                {
                    var entry = ReadEntryFromWorksheet(worksheet, row, columns);
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue with other rows
                    Console.WriteLine($"Warnung: Zeile {row} konnte nicht gelesen werden: {ex.Message}");
                }
            }

            if (entries.Count == 0)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Keine gültigen Einträge in der Excel-Datei gefunden"
                };
            }

            return new ServiceResult
            {
                Success = true,
                Message = $"{entries.Count} Einträge geladen",
                Data = entries
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

            if (!File.Exists(filePath))
            {
                return new ServiceResult { Success = false, Message = "Datei nicht gefunden" };
            }

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);

            var headerRow = 1;
            var columns = FindColumnIndices(worksheet, headerRow);

            // Clear existing data rows (keep header)
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;
            if (lastRow > headerRow)
            {
                worksheet.Rows(headerRow + 1, lastRow).Delete();
            }

            // Write all entries
            var currentRow = headerRow + 1;
            foreach (var entry in bereitschaften.OrderBy(e => e.Datum).ThenBy(e => e.GruppeName))
            {
                WriteEntryToWorksheet(worksheet, currentRow, entry, entry.Datum, columns);
                currentRow++;
            }

            // Save workbook
            workbook.SaveAs(filePath);

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
    // HELPER METHODS - Excel Operations
    // ============================================================================

    /// <summary>
    /// Finds the template file in various possible locations
    /// </summary>
    private string? FindTemplatePath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", TEMPLATE_FILENAME),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "config", TEMPLATE_FILENAME),
            Path.Combine(Environment.CurrentDirectory, "config", TEMPLATE_FILENAME),
            Path.Combine("/root/BereitschaftsPlaner-Avalonia-PoC", "config", TEMPLATE_FILENAME)
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds column indices from header row (case-insensitive)
    /// </summary>
    private Dictionary<string, int> FindColumnIndices(IXLWorksheet worksheet, int headerRow)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var lastCol = worksheet.Row(headerRow).LastCellUsed()?.Address.ColumnNumber ?? 1;

        for (int col = 1; col <= lastCol; col++)
        {
            var headerValue = worksheet.Cell(headerRow, col).GetString().Trim();
            if (!string.IsNullOrEmpty(headerValue))
            {
                columns[headerValue] = col;
            }
        }

        return columns;
    }

    /// <summary>
    /// Writes a BereitschaftEntry to a worksheet row
    /// </summary>
    private void WriteEntryToWorksheet(IXLWorksheet worksheet, int row, BereitschaftEntry entry, DateTime date, Dictionary<string, int>? columns = null)
    {
        // Common D365 column names (will auto-detect from template)
        var colMap = columns ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Helper to set cell value if column exists
        void SetCell(string columnName, object value)
        {
            if (colMap.TryGetValue(columnName, out var colIndex))
            {
                worksheet.Cell(row, colIndex).Value = value;
            }
        }

        // Map entry fields to Excel columns
        SetCell("Name", entry.GruppeName); // Bereitschaftsgruppe
        SetCell("Bookable Resource", entry.RessourceName); // Ressource/Person
        SetCell("Start Time", $"{date:dd.MM.yyyy} {entry.StartZeit}"); // Startzeit
        SetCell("End Time", CalculateEndDateTime(date, entry.StartZeit, entry.EndZeit)); // Endzeit

        // Additional fields that might exist
        SetCell("Booking Status", "Committed");
        SetCell("Duration", CalculateDuration(entry.StartZeit, entry.EndZeit));
    }

    /// <summary>
    /// Reads a BereitschaftEntry from a worksheet row
    /// </summary>
    private BereitschaftEntry? ReadEntryFromWorksheet(IXLWorksheet worksheet, int row, Dictionary<string, int> columns)
    {
        string GetCell(string columnName)
        {
            if (columns.TryGetValue(columnName, out var colIndex))
            {
                return worksheet.Cell(row, colIndex).GetString().Trim();
            }
            return string.Empty;
        }

        var name = GetCell("Name");
        if (string.IsNullOrEmpty(name))
        {
            return null; // Skip empty rows
        }

        var startTimeStr = GetCell("Start Time");
        var endTimeStr = GetCell("End Time");

        // Parse date and time
        DateTime? startDateTime = ParseDateTime(startTimeStr);
        DateTime? endDateTime = ParseDateTime(endTimeStr);

        if (!startDateTime.HasValue)
        {
            return null;
        }

        var entry = new BereitschaftEntry
        {
            Id = row - 1, // Use row number as ID
            Datum = startDateTime.Value.Date,
            GruppeName = name,
            RessourceName = GetCell("Bookable Resource"),
            StartZeit = startDateTime.Value.ToString("HH:mm"),
            EndZeit = endDateTime?.ToString("HH:mm") ?? "07:30",
            Typ = DetermineTyp(startDateTime.Value.ToString("HH:mm"))
        };

        return entry;
    }

    // ============================================================================
    // HELPER METHODS - Business Logic
    // ============================================================================

    /// <summary>
    /// Determines service type and times for a specific date based on Zeitprofil
    /// </summary>
    private async Task<(string? StartZeit, string? EndZeit, string? Typ)> GetDienstForDateAsync(
        DateTime date,
        Zeitprofil? zeitprofil,
        FeiertagsService feiertagsService)
    {
        if (zeitprofil == null)
        {
            // Default: Bereitschaftsdienst (BD) every day
            return ("16:00", "07:30", "BD");
        }

        var dayName = date.ToString("dddd", new System.Globalization.CultureInfo("de-DE"));

        // Check if it's a holiday
        bool isHoliday = await feiertagsService.IstFeiertagAsync(
            date,
            zeitprofil.Feiertage.Bundesland,
            zeitprofil.Feiertage.Region
        );

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
    /// Calculates end date/time considering overnight shifts
    /// </summary>
    private string CalculateEndDateTime(DateTime startDate, string startZeit, string endZeit)
    {
        var start = TimeSpan.Parse(startZeit);
        var end = TimeSpan.Parse(endZeit);

        // If end time is before start time, it's next day
        var endDate = end <= start ? startDate.AddDays(1) : startDate;

        return $"{endDate:dd.MM.yyyy} {endZeit}";
    }

    /// <summary>
    /// Calculates duration in minutes
    /// </summary>
    private int CalculateDuration(string startZeit, string endZeit)
    {
        var start = TimeSpan.Parse(startZeit);
        var end = TimeSpan.Parse(endZeit);

        // If end time is before start time, it's next day
        var duration = end <= start ? (TimeSpan.FromHours(24) - start + end) : (end - start);

        return (int)duration.TotalMinutes;
    }

    /// <summary>
    /// Parses a date/time string from Excel
    /// </summary>
    private DateTime? ParseDateTime(string dateTimeStr)
    {
        if (string.IsNullOrWhiteSpace(dateTimeStr))
        {
            return null;
        }

        // Try various formats
        var formats = new[]
        {
            "dd.MM.yyyy HH:mm",
            "dd/MM/yyyy HH:mm",
            "yyyy-MM-dd HH:mm",
            "MM/dd/yyyy HH:mm"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateTimeStr, format, null, System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }
        }

        // Try general parse
        if (DateTime.TryParse(dateTimeStr, out var generalResult))
        {
            return generalResult;
        }

        return null;
    }

    /// <summary>
    /// Determines BD or TD based on start time
    /// </summary>
    private string DetermineTyp(string startZeit)
    {
        var time = TimeSpan.Parse(startZeit);

        // BD typically starts in afternoon/evening (after 12:00)
        // TD typically starts in morning (before 12:00)
        return time.Hours >= 12 ? "BD" : "TD";
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