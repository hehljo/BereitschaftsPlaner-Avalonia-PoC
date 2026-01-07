using System;
using System.Collections.Generic;
using LiteDB;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a saved planning template that can be reused
/// </summary>
public class PlanningTemplate
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category (e.g., "Sommer", "Winter", "Urlaubszeit")
    /// </summary>
    public string Category { get; set; } = "Standard";

    /// <summary>
    /// Template creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Source month (for reference)
    /// </summary>
    public DateTime SourceMonth { get; set; }

    /// <summary>
    /// Assignment type (BD or TD)
    /// </summary>
    public string Typ { get; set; } = "BD";

    /// <summary>
    /// Saved assignments (day-of-month -> resource name)
    /// </summary>
    public Dictionary<int, AssignmentData> Assignments { get; set; } = new();

    /// <summary>
    /// Display text for UI
    /// </summary>
    [BsonIgnore]
    public string DisplayText => $"{Name} ({Category}) - {CreatedAt:dd.MM.yyyy}";

    /// <summary>
    /// Assignment count
    /// </summary>
    [BsonIgnore]
    public int AssignmentCount => Assignments.Count;
}

/// <summary>
/// Simplified assignment data for template storage
/// </summary>
public class AssignmentData
{
    public string GruppeName { get; set; } = string.Empty;
    public string RessourceName { get; set; } = string.Empty;
    public string StartZeit { get; set; } = "16:00";
    public string EndZeit { get; set; } = "07:30";
}
