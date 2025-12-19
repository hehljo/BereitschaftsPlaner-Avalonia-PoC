using System;
using System.Collections.Generic;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Time profile defining Bereitschaftsdienst and Tagesdienst schedules
/// Matches PowerShell implementation structure
/// </summary>
public class Zeitprofil
{
    public int Id { get; set; }
    public string ProfilID { get; set; } = string.Empty;  // Unique identifier (e.g. "Standard", "Augsburg")
    public string Name { get; set; } = string.Empty;  // Display name

    // Bereitschaftsdienst (BD) - on-call days (16:00-07:30)
    public List<DienstTag> BereitschaftsTage { get; set; } = new();

    // Tagesdienst (TD) - day shifts (07:30-16:00)
    public List<DienstTag> Tagesdienste { get; set; } = new();

    // Holiday configuration
    public FeiertagsKonfiguration Feiertage { get; set; } = new();

    // Default type for undefined days ("BD" or "TD")
    public string StandardTypFuerUndefiniert { get; set; } = "BD";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public override string ToString() => Name;
}
