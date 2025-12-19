using System;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a single on-call duty entry
/// </summary>
public class Bereitschaft
{
    public int Id { get; set; }
    public string GruppeName { get; set; } = string.Empty;
    public string RessourcenName { get; set; } = string.Empty;
    public string Bezirk { get; set; } = string.Empty;
    public DateTime StartDatum { get; set; }
    public DateTime EndDatum { get; set; }
    public string ZeitprofilName { get; set; } = string.Empty;
    public bool IstFeiertag { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public override string ToString() =>
        $"{RessourcenName} ({GruppeName}): {StartDatum:dd.MM.yyyy HH:mm} - {EndDatum:dd.MM.yyyy HH:mm}";
}
