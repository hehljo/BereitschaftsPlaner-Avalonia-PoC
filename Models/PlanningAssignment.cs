using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a resource assignment for a specific day and group
/// Used in the Visual Planning Board
/// </summary>
public partial class PlanningAssignment : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private string _gruppeName = string.Empty;

    [ObservableProperty]
    private string _ressourceName = string.Empty;

    [ObservableProperty]
    private string _typ = "BD"; // BD or TD

    [ObservableProperty]
    private string _startZeit = "16:00";

    [ObservableProperty]
    private string _endZeit = "07:30";

    [ObservableProperty]
    private bool _hasConflict = false;

    /// <summary>
    /// Color for visual representation (based on group)
    /// </summary>
    public string GroupColor
    {
        get
        {
            // Generate deterministic color from group name
            var hash = GruppeName.GetHashCode();
            var hue = Math.Abs(hash % 360);
            return $"hsl({hue}, 65%, 75%)";
        }
    }

    /// <summary>
    /// Display text for assignment card
    /// </summary>
    public string DisplayText => $"{RessourceName}\n{Typ} {StartZeit}-{EndZeit}";

    /// <summary>
    /// Week number for this assignment (ISO 8601)
    /// </summary>
    public int WeekNumber => Kalenderwoche.GetWeekNumber(Date);

    /// <summary>
    /// Unique identifier for this assignment
    /// </summary>
    public string AssignmentId => $"{Date:yyyyMMdd}_{GruppeName}_{Typ}";
}
