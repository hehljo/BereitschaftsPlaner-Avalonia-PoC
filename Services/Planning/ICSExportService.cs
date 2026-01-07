using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BereitschaftsPlaner.Avalonia.Models;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.Services.Planning;

/// <summary>
/// Service for exporting assignments to ICS (iCalendar) format
/// Compatible with Outlook, Google Calendar, Apple Calendar, etc.
/// </summary>
public class ICSExportService
{
    /// <summary>
    /// Export assignments to ICS file
    /// </summary>
    public void ExportToICS(List<PlanningAssignment> assignments, string outputPath, string calendarName = "Bereitschaftsdienste")
    {
        try
        {
            var sb = new StringBuilder();

            // ICS Header
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//BereitschaftsPlaner//Avalonia//DE");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");
            sb.AppendLine($"X-WR-CALNAME:{EscapeICS(calendarName)}");
            sb.AppendLine("X-WR-TIMEZONE:Europe/Berlin");
            sb.AppendLine("X-WR-CALDESC:Automatisch generiert aus BereitschaftsPlaner");

            // Add timezone info for Europe/Berlin
            sb.AppendLine("BEGIN:VTIMEZONE");
            sb.AppendLine("TZID:Europe/Berlin");
            sb.AppendLine("BEGIN:DAYLIGHT");
            sb.AppendLine("TZOFFSETFROM:+0100");
            sb.AppendLine("TZOFFSETTO:+0200");
            sb.AppendLine("TZNAME:CEST");
            sb.AppendLine("DTSTART:19700329T020000");
            sb.AppendLine("RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=-1SU");
            sb.AppendLine("END:DAYLIGHT");
            sb.AppendLine("BEGIN:STANDARD");
            sb.AppendLine("TZOFFSETFROM:+0200");
            sb.AppendLine("TZOFFSETTO:+0100");
            sb.AppendLine("TZNAME:CET");
            sb.AppendLine("DTSTART:19701025T030000");
            sb.AppendLine("RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU");
            sb.AppendLine("END:STANDARD");
            sb.AppendLine("END:VTIMEZONE");

            // Add events for each assignment
            foreach (var assignment in assignments.OrderBy(a => a.Date))
            {
                sb.AppendLine(CreateVEvent(assignment));
            }

            sb.AppendLine("END:VCALENDAR");

            // Write to file with UTF-8 encoding
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);

            Log.Information("ICS Export erfolgreich: {Path}, {Count} Ereignisse",
                outputPath, assignments.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim ICS-Export");
            throw;
        }
    }

    /// <summary>
    /// Create a VEVENT block for a single assignment
    /// </summary>
    private string CreateVEvent(PlanningAssignment assignment)
    {
        var sb = new StringBuilder();

        // Parse times
        var startTime = TimeSpan.Parse(assignment.StartZeit);
        var endTime = TimeSpan.Parse(assignment.EndZeit);

        // Calculate start and end DateTime
        var startDateTime = assignment.Date.Add(startTime);

        // If end time is before start time, it's next day
        var endDateTime = endTime <= startTime
            ? assignment.Date.AddDays(1).Add(endTime)
            : assignment.Date.Add(endTime);

        // Generate unique ID (using AssignmentId if available, otherwise generate)
        var uid = $"{assignment.AssignmentId}@bereitschaftsplaner.local";

        // Event summary and description
        var summary = $"{assignment.Typ} - {assignment.GruppeName}";
        var description = $"Ressource: {assignment.RessourceName}\\nGruppe: {assignment.GruppeName}\\nTyp: {assignment.Typ}\\nZeit: {assignment.StartZeit} - {assignment.EndZeit}";

        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{uid}");
        sb.AppendLine($"DTSTAMP:{FormatICSDateTime(DateTime.Now)}");
        sb.AppendLine($"DTSTART;TZID=Europe/Berlin:{FormatICSDateTime(startDateTime)}");
        sb.AppendLine($"DTEND;TZID=Europe/Berlin:{FormatICSDateTime(endDateTime)}");
        sb.AppendLine($"SUMMARY:{EscapeICS(summary)}");
        sb.AppendLine($"DESCRIPTION:{EscapeICS(description)}");
        sb.AppendLine($"LOCATION:{EscapeICS(assignment.GruppeName)}");
        sb.AppendLine("STATUS:CONFIRMED");
        sb.AppendLine("TRANSP:OPAQUE"); // Blocks time in calendar
        sb.AppendLine("SEQUENCE:0");

        // Add category/color based on type
        var category = assignment.Typ == "BD" ? "BEREITSCHAFTSDIENST" : "TAGESDIENST";
        sb.AppendLine($"CATEGORIES:{category}");

        // Add alarm/reminder 24 hours before
        sb.AppendLine("BEGIN:VALARM");
        sb.AppendLine("TRIGGER:-PT24H"); // 24 hours before
        sb.AppendLine("DESCRIPTION:Erinnerung: Bereitschaftsdienst morgen");
        sb.AppendLine("ACTION:DISPLAY");
        sb.AppendLine("END:VALARM");

        sb.AppendLine("END:VEVENT");

        return sb.ToString();
    }

    /// <summary>
    /// Format DateTime for ICS (yyyyMMddTHHmmss)
    /// </summary>
    private string FormatICSDateTime(DateTime dt)
    {
        return dt.ToString("yyyyMMddTHHmmss");
    }

    /// <summary>
    /// Escape special characters for ICS format
    /// </summary>
    private string EscapeICS(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")  // Backslash must be escaped first
            .Replace(";", "\\;")    // Semicolon
            .Replace(",", "\\,")    // Comma
            .Replace("\n", "\\n")   // Newline
            .Replace("\r", "");     // Remove carriage return
    }

    /// <summary>
    /// Export assignments for a specific person to ICS
    /// </summary>
    public void ExportPersonalICS(List<PlanningAssignment> allAssignments, string personName, string outputPath)
    {
        var personAssignments = allAssignments
            .Where(a => a.RessourceName.Equals(personName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!personAssignments.Any())
        {
            throw new InvalidOperationException($"Keine Zuordnungen für {personName} gefunden.");
        }

        ExportToICS(personAssignments, outputPath, $"Bereitschaftsdienste - {personName}");
    }

    /// <summary>
    /// Generate ICS content as string (for web/email delivery)
    /// </summary>
    public string GenerateICSString(List<PlanningAssignment> assignments, string calendarName = "Bereitschaftsdienste")
    {
        var tempPath = Path.GetTempFileName() + ".ics";
        ExportToICS(assignments, tempPath, calendarName);
        var content = File.ReadAllText(tempPath, Encoding.UTF8);
        File.Delete(tempPath);
        return content;
    }
}
