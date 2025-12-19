using System;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Holiday configuration for a Zeitprofil
/// </summary>
public class FeiertagsKonfiguration
{
    public string Bundesland { get; set; } = "BY";  // BY, BW, BE, etc.
    public string Region { get; set; } = string.Empty;  // e.g. "Augsburg" for Bavaria
    public string BehandelnWie { get; set; } = "Sonntag";  // Treat holidays like "Sonntag" or "Samstag"
}
