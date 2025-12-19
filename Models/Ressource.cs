using System;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a resource (employee) with name and district
/// </summary>
public class Ressource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bezirk { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public override string ToString() => $"{Name} ({Bezirk})";
}
