using System.Collections.Generic;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Validates import data before saving to database
/// </summary>
public class DataValidator
{
    /// <summary>
    /// Validates a list of Ressourcen
    /// </summary>
    public ValidationResult ValidateRessourcen(List<Ressource> ressourcen)
    {
        var result = new ValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        if (ressourcen == null || ressourcen.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("Keine Ressourcen zum Importieren gefunden");
            return result;
        }

        // Check for duplicates
        var duplicates = ressourcen
            .GroupBy(r => r.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            result.Errors.Add($"Duplikate gefunden: {string.Join(", ", duplicates.Take(5))}");
            result.WarningCount = duplicates.Count;
        }

        // Check for empty names
        var emptyNames = ressourcen.Count(r => string.IsNullOrWhiteSpace(r.Name));
        if (emptyNames > 0)
        {
            result.Errors.Add($"{emptyNames} Ressourcen haben keinen Namen (werden übersprungen)");
            result.WarningCount += emptyNames;
        }

        // Check for missing Bezirk
        var missingBezirk = ressourcen.Count(r => string.IsNullOrWhiteSpace(r.Bezirk));
        if (missingBezirk > 0)
        {
            result.Errors.Add($"WARNUNG: {missingBezirk} Ressourcen haben keinen Bezirk");
            result.WarningCount += missingBezirk;
        }

        return result;
    }

    /// <summary>
    /// Validates a list of Bereitschaftsgruppen
    /// </summary>
    public ValidationResult ValidateBereitschaftsGruppen(List<BereitschaftsGruppe> gruppen)
    {
        var result = new ValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        if (gruppen == null || gruppen.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("Keine Bereitschaftsgruppen zum Importieren gefunden");
            return result;
        }

        // Check for duplicates
        var duplicates = gruppen
            .GroupBy(g => g.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            result.Errors.Add($"Duplikate gefunden: {string.Join(", ", duplicates.Take(5))}");
            result.WarningCount = duplicates.Count;
        }

        // Check for empty names
        var emptyNames = gruppen.Count(g => string.IsNullOrWhiteSpace(g.Name));
        if (emptyNames > 0)
        {
            result.Errors.Add($"{emptyNames} Gruppen haben keinen Namen (werden übersprungen)");
            result.WarningCount += emptyNames;
        }

        // Check for missing Bezirk
        var missingBezirk = gruppen.Count(g => string.IsNullOrWhiteSpace(g.Bezirk));
        if (missingBezirk > 0)
        {
            result.Errors.Add($"WARNUNG: {missingBezirk} Gruppen haben keinen Bezirk");
            result.WarningCount += missingBezirk;
        }

        // Check for missing Verantwortliche Person
        var missingOwner = gruppen.Count(g => string.IsNullOrWhiteSpace(g.VerantwortlichePerson));
        if (missingOwner > 0)
        {
            result.Errors.Add($"WARNUNG: {missingOwner} Gruppen haben keine verantwortliche Person");
            result.WarningCount += missingOwner;
        }

        return result;
    }

    /// <summary>
    /// Cleans data by removing invalid entries
    /// </summary>
    public List<Ressource> CleanRessourcen(List<Ressource> ressourcen)
    {
        return ressourcen
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => r.Name)
            .Select(g => g.First()) // Remove duplicates, keep first
            .ToList();
    }

    /// <summary>
    /// Cleans data by removing invalid entries
    /// </summary>
    public List<BereitschaftsGruppe> CleanBereitschaftsGruppen(List<BereitschaftsGruppe> gruppen)
    {
        return gruppen
            .Where(g => !string.IsNullOrWhiteSpace(g.Name))
            .GroupBy(g => g.Name)
            .Select(g => g.First()) // Remove duplicates, keep first
            .ToList();
    }
}
