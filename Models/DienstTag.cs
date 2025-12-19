using System;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Configuration for a single day (Bereitschaftstag or Tagesdienst)
/// </summary>
public class DienstTag
{
    public string Tag { get; set; } = string.Empty;  // Montag, Dienstag, etc.
    public string Von { get; set; } = string.Empty;  // e.g. "16:00" or "07:30"
    public string Bis { get; set; } = string.Empty;  // e.g. "07:30" or "16:00"
}
