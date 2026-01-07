using System;
using LiteDB;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a vacation or unavailable day for a resource
/// </summary>
public class VacationDay
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    public string RessourceName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public VacationType Type { get; set; } = VacationType.Vacation;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string TypeIcon => Type switch
    {
        VacationType.Vacation => "🏖️",
        VacationType.Sick => "🤒",
        VacationType.Training => "📚",
        VacationType.Other => "🚫",
        _ => "❓"
    };

    public string TypeDisplayName => Type switch
    {
        VacationType.Vacation => "Urlaub",
        VacationType.Sick => "Krank",
        VacationType.Training => "Fortbildung",
        VacationType.Other => "Sonstiges",
        _ => "Unbekannt"
    };

    public string DisplayText => $"{TypeIcon} {RessourceName} - {TypeDisplayName}";
}

/// <summary>
/// Types of unavailability
/// </summary>
public enum VacationType
{
    Vacation = 0,    // Regular vacation
    Sick = 1,        // Sick leave
    Training = 2,    // Training/courses
    Other = 3        // Other reasons
}
