using System;
using System.Collections.Generic;
using LiteDB;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a draft planning scenario ("What-If")
/// Allows users to create multiple versions and compare them
/// </summary>
public class PlanningScenario
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    /// <summary>
    /// Scenario name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description (e.g., "Was wäre wenn Person X im Urlaub ist?")
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Month this scenario applies to
    /// </summary>
    public DateTime Month { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Assignment type (BD or TD)
    /// </summary>
    public string Typ { get; set; } = "BD";

    /// <summary>
    /// Stored assignments for this scenario
    /// </summary>
    public List<PlanningAssignment> Assignments { get; set; } = new();

    /// <summary>
    /// Whether this scenario is marked as "active" or "baseline"
    /// </summary>
    public bool IsBaseline { get; set; } = false;

    /// <summary>
    /// Fairness score for this scenario (0-100)
    /// </summary>
    public double FairnessScore { get; set; } = 0;

    /// <summary>
    /// Number of conflicts in this scenario
    /// </summary>
    public int ConflictCount { get; set; } = 0;

    /// <summary>
    /// Display text for UI
    /// </summary>
    [BsonIgnore]
    public string DisplayText =>
        $"{Name} ({Month:MMMM yyyy}) - {Assignments.Count} Zuordnungen - Fairness: {FairnessScore:F0}%";

    /// <summary>
    /// Color indicator based on fairness score
    /// </summary>
    [BsonIgnore]
    public string StatusColor => FairnessScore switch
    {
        >= 90 => "#4CAF50",  // Green
        >= 70 => "#FFC107",  // Yellow
        _ => "#F44336"       // Red
    };

    /// <summary>
    /// Status emoji
    /// </summary>
    [BsonIgnore]
    public string StatusIcon => FairnessScore switch
    {
        >= 90 => "✅",
        >= 70 => "⚠️",
        _ => "❌"
    };
}
