namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a resource (employee) with name and district
/// </summary>
public class Ressource
{
    public string Name { get; set; } = string.Empty;
    public string Bezirk { get; set; } = string.Empty;

    public override string ToString() => $"{Name} ({Bezirk})";
}
