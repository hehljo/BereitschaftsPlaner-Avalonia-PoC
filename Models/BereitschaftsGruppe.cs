using System;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a Bereitschafts group (on-call duty group)
/// </summary>
public class BereitschaftsGruppe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bezirk { get; set; } = string.Empty;
    public string VerantwortlichePerson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public override string ToString() => $"{Name} - Bezirk: {Bezirk}";
}
