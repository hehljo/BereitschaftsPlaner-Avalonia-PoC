using System;
using System.Globalization;

namespace BereitschaftsPlaner.Avalonia.Models;

/// <summary>
/// Represents a calendar week (Kalenderwoche)
/// </summary>
public class Kalenderwoche
{
    public int Jahr { get; set; }
    public int Woche { get; set; }
    public bool IsSelected { get; set; }
    
    public string DisplayText => $"KW {Woche}";
    public string DisplayTextLong => $"KW {Woche} ({Jahr})";
    
    public DateTime StartDatum { get; set; }
    public DateTime EndDatum { get; set; }
    
    public string DateRange => $"{StartDatum:dd.MM} - {EndDatum:dd.MM.yyyy}";

    public Kalenderwoche(int jahr, int woche)
    {
        Jahr = jahr;
        Woche = woche;
        IsSelected = false;
        
        // Berechne Start- und Enddatum der KW
        var (start, end) = GetWeekDates(jahr, woche);
        StartDatum = start;
        EndDatum = end;
    }

    /// <summary>
    /// Berechnet Start- und Enddatum einer Kalenderwoche nach ISO 8601
    /// </summary>
    private static (DateTime Start, DateTime End) GetWeekDates(int year, int weekNumber)
    {
        // ISO 8601: Erste KW enthält den ersten Donnerstag des Jahres
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

        if (daysOffset < 0)
            daysOffset += 7;

        var firstThursday = jan1.AddDays(daysOffset);
        var cal = CultureInfo.CurrentCulture.Calendar;
        var firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        var weekNum = weekNumber;
        if (firstWeek == 1)
            weekNum -= 1;

        var result = firstThursday.AddDays((weekNum) * 7);
        
        // Montag der Woche
        var monday = result.AddDays(-(int)result.DayOfWeek + (int)DayOfWeek.Monday);
        if (result.DayOfWeek == DayOfWeek.Sunday)
            monday = monday.AddDays(-7);
            
        var sunday = monday.AddDays(6);

        return (monday, sunday);
    }

    /// <summary>
    /// Ermittelt die Kalenderwoche für ein Datum nach ISO 8601
    /// </summary>
    public static int GetWeekNumber(DateTime date)
    {
        var day = CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            date = date.AddDays(3);

        return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            date, 
            CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday
        );
    }

    /// <summary>
    /// Ermittelt die Anzahl der Kalenderwochen in einem Jahr
    /// </summary>
    public static int GetWeeksInYear(int year)
    {
        var dec31 = new DateTime(year, 12, 31);
        var weekNum = GetWeekNumber(dec31);
        
        // Wenn KW 1 des Folgejahres, dann hat das Jahr 52 Wochen
        return weekNum == 1 ? 52 : weekNum;
    }
}
