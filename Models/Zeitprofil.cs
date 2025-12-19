namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a time profile (BD/TD) with weekly schedule configuration
/// </summary>
public class Zeitprofil
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Bereitschaftsdienst", "Tagdienst"
    public string StartZeit { get; set; } = "16:00"; // Default BD start time
    public string EndZeit { get; set; } = "07:30"; // Default BD end time
    public bool Folgetag { get; set; } = true; // End time is next day

    // Weekly schedule (which days this profile applies to)
    public bool Montag { get; set; } = true;
    public bool Dienstag { get; set; } = true;
    public bool Mittwoch { get; set; } = true;
    public bool Donnerstag { get; set; } = true;
    public bool Freitag { get; set; } = true;
    public bool Samstag { get; set; } = true;
    public bool Sonntag { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public override string ToString() => $"{Name} ({StartZeit}-{EndZeit})";
}
