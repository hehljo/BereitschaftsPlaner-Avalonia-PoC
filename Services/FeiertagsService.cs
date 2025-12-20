using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for German public holidays (Feiertage)
/// Supports API loading, local calculation fallback, and JSON persistence
/// </summary>
public class FeiertagsService
{
    private const string API_URL = "https://feiertage-api.de/api/";
    private readonly string _feiertagsPath;
    private readonly Dictionary<string, List<Feiertag>> _cache;
    private readonly HttpClient _httpClient;

    public FeiertagsService()
    {
        // Setup feiertage directory
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        _feiertagsPath = Path.Combine(configPath, "feiertage");

        if (!Directory.Exists(_feiertagsPath))
        {
            Directory.CreateDirectory(_feiertagsPath);
        }

        _cache = new Dictionary<string, List<Feiertag>>();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    /// <summary>
    /// Get holidays for a year with smart caching (Memory -> File -> API -> Fallback)
    /// </summary>
    public async Task<List<Feiertag>> GetFeiertageFuerJahrAsync(
        int jahr,
        string bundesland,
        string region = "",
        bool forceReload = false)
    {
        var cacheKey = BuildCacheKey(jahr, bundesland, region);

        // 1. Memory cache (fastest)
        if (!forceReload && _cache.ContainsKey(cacheKey))
        {
            return _cache[cacheKey];
        }

        // 2. Persistent file
        var filePath = GetFeiertagsDateiPfad(jahr, bundesland, region);
        if (!forceReload && File.Exists(filePath))
        {
            try
            {
                var feiertage = await LoadFromFileAsync(filePath);
                if (feiertage != null && feiertage.Count > 0)
                {
                    _cache[cacheKey] = feiertage;
                    return feiertage;
                }
            }
            catch
            {
                // Continue to API
            }
        }

        // 3. API attempt
        try
        {
            var feiertage = await GetFeiertagVonAPIAsync(jahr, bundesland);
            if (feiertage != null && feiertage.Count > 0)
            {
                await SaveToFileAsync(feiertage, jahr, bundesland, region);
                _cache[cacheKey] = feiertage;
                return feiertage;
            }
        }
        catch
        {
            // Continue to fallback
        }

        // 4. Local calculation fallback
        var lokaleFeiertage = GetFeiertagLokal(jahr, bundesland);
        await SaveToFileAsync(lokaleFeiertage, jahr, bundesland, region);
        _cache[cacheKey] = lokaleFeiertage;

        return lokaleFeiertage;
    }

    /// <summary>
    /// Check if a date is a holiday
    /// </summary>
    public async Task<bool> IstFeiertagAsync(DateTime datum, string bundesland, string region = "")
    {
        var feiertage = await GetFeiertageFuerJahrAsync(datum.Year, bundesland, region);
        return feiertage.Any(f => f.Datum.Date == datum.Date);
    }

    /// <summary>
    /// Get list of all saved holiday files
    /// </summary>
    public List<FeiertagsDateiInfo> GetGespeicherteFeiertage()
    {
        if (!Directory.Exists(_feiertagsPath))
            return new List<FeiertagsDateiInfo>();

        var files = Directory.GetFiles(_feiertagsPath, "feiertage_*.json");
        var result = new List<FeiertagsDateiInfo>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            // Parse: feiertage_BY_2025.json or feiertage_BY_2025_Augsburg.json
            var match = System.Text.RegularExpressions.Regex.Match(
                fileName,
                @"^feiertage_([A-Z]{2})_(\d{4})(?:_(.+))?\.json$"
            );

            if (match.Success)
            {
                result.Add(new FeiertagsDateiInfo
                {
                    Bundesland = match.Groups[1].Value,
                    Jahr = int.Parse(match.Groups[2].Value),
                    Region = match.Groups[3].Success ? match.Groups[3].Value.Replace("_", " ") : "",
                    DateiName = fileName,
                    Pfad = file
                });
            }
        }

        return result.OrderBy(f => f.Jahr).ThenBy(f => f.Bundesland).ToList();
    }

    /// <summary>
    /// Delete a holiday file
    /// </summary>
    public bool DeleteFeiertagsDatei(int jahr, string bundesland, string region = "")
    {
        var filePath = GetFeiertagsDateiPfad(jahr, bundesland, region);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);

                // Remove from cache
                var cacheKey = BuildCacheKey(jahr, bundesland, region);
                _cache.Remove(cacheKey);

                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    // ============================================================================
    // FILE OPERATIONS
    // ============================================================================

    private string GetFeiertagsDateiPfad(int jahr, string bundesland, string region)
    {
        var fileName = $"feiertage_{bundesland}_{jahr}";

        if (!string.IsNullOrWhiteSpace(region) && region != "(Keine)")
        {
            var regionClean = System.Text.RegularExpressions.Regex.Replace(region, @"[^\w]", "_");
            fileName += $"_{regionClean}";
        }

        fileName += ".json";

        return Path.Combine(_feiertagsPath, fileName);
    }

    private async Task<List<Feiertag>?> LoadFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<List<FeiertagJson>>(json);

        if (data == null) return null;

        return data.Select(f => new Feiertag
        {
            Name = f.Name,
            Datum = DateTime.Parse(f.Datum),
            Hinweis = f.Hinweis ?? "",
            Quelle = $"Gespeichert ({f.Quelle})"
        }).ToList();
    }

    private async Task SaveToFileAsync(List<Feiertag> feiertage, int jahr, string bundesland, string region)
    {
        try
        {
            var filePath = GetFeiertagsDateiPfad(jahr, bundesland, region);

            var exportData = feiertage.Select(f => new FeiertagJson
            {
                Name = f.Name,
                Datum = f.Datum.ToString("yyyy-MM-dd"),
                Hinweis = f.Hinweis,
                Quelle = f.Quelle
            }).ToList();

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    // ============================================================================
    // API INTEGRATION
    // ============================================================================

    private async Task<List<Feiertag>?> GetFeiertagVonAPIAsync(int jahr, string bundesland)
    {
        try
        {
            var url = $"{API_URL}?jahr={jahr}&nur_land={bundesland}";
            var response = await _httpClient.GetStringAsync(url);

            var apiData = JsonSerializer.Deserialize<Dictionary<string, FeiertagApiResponse>>(response);
            if (apiData == null) return null;

            var feiertage = new List<Feiertag>();

            foreach (var kvp in apiData)
            {
                if (kvp.Value == null || string.IsNullOrEmpty(kvp.Value.datum))
                    continue;

                feiertage.Add(new Feiertag
                {
                    Name = kvp.Key,
                    Datum = DateTime.Parse(kvp.Value.datum),
                    Hinweis = kvp.Value.hinweis ?? "",
                    Quelle = "API"
                });
            }

            return feiertage.OrderBy(f => f.Datum).ToList();
        }
        catch
        {
            return null;
        }
    }

    // ============================================================================
    // LOCAL CALCULATION (FALLBACK)
    // ============================================================================

    private List<Feiertag> GetFeiertagLokal(int jahr, string bundesland)
    {
        var feiertage = new List<Feiertag>();

        // Calculate Easter Sunday (Gauss algorithm)
        var ostern = BerechnOstersonntag(jahr);

        // === BUNDESWEIT (Nationwide) ===
        feiertage.Add(new Feiertag { Name = "Neujahr", Datum = new DateTime(jahr, 1, 1), Quelle = "Lokal" });
        feiertage.Add(new Feiertag { Name = "Karfreitag", Datum = ostern.AddDays(-2), Quelle = "Lokal" });
        feiertage.Add(new Feiertag { Name = "Ostermontag", Datum = ostern.AddDays(1), Quelle = "Lokal" });
        feiertage.Add(new Feiertag { Name = "Tag der Arbeit", Datum = new DateTime(jahr, 5, 1), Quelle = "Lokal" });
        feiertage.Add(new Feiertag { Name = "Christi Himmelfahrt", Datum = ostern.AddDays(39), Quelle = "Lokal" });
        feiertage.Add(new Feiertag { Name = "Pfingstmontag", Datum = ostern.AddDays(50), Quelle = "Lokal" });
        feiertage.Add(new Feiertag { Name = "Tag der Deutschen Einheit", Datum = new DateTime(jahr, 10, 3), Quelle = "Lokal" });
        feiertage.Add(new Feiertag { Name = "1. Weihnachtstag", Datum = new DateTime(jahr, 12, 25), Quelle = "Lokal" });
        feiertage.Add(new Feiertag { Name = "2. Weihnachtstag", Datum = new DateTime(jahr, 12, 26), Quelle = "Lokal" });

        // === BUNDESLAND-SPECIFIC ===
        switch (bundesland)
        {
            case "BW": // Baden-Württemberg
                feiertage.Add(new Feiertag { Name = "Heilige Drei Könige", Datum = new DateTime(jahr, 1, 6), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Fronleichnam", Datum = ostern.AddDays(60), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Allerheiligen", Datum = new DateTime(jahr, 11, 1), Quelle = "Lokal" });
                break;

            case "BY": // Bayern
                feiertage.Add(new Feiertag { Name = "Heilige Drei Könige", Datum = new DateTime(jahr, 1, 6), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Fronleichnam", Datum = ostern.AddDays(60), Quelle = "Lokal" });
                feiertage.Add(new Feiertag
                {
                    Name = "Mariä Himmelfahrt",
                    Datum = new DateTime(jahr, 8, 15),
                    Hinweis = "Nur in überwiegend katholischen Gemeinden",
                    Quelle = "Lokal"
                });
                feiertage.Add(new Feiertag { Name = "Allerheiligen", Datum = new DateTime(jahr, 11, 1), Quelle = "Lokal" });
                break;

            case "BE": // Berlin
                feiertage.Add(new Feiertag { Name = "Internationaler Frauentag", Datum = new DateTime(jahr, 3, 8), Quelle = "Lokal" });
                break;

            case "BB": // Brandenburg
                feiertage.Add(new Feiertag { Name = "Ostersonntag", Datum = ostern, Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Pfingstsonntag", Datum = ostern.AddDays(49), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Reformationstag", Datum = new DateTime(jahr, 10, 31), Quelle = "Lokal" });
                break;

            case "HB": // Bremen
            case "HH": // Hamburg
            case "MV": // Mecklenburg-Vorpommern
            case "NI": // Niedersachsen
            case "SH": // Schleswig-Holstein
                feiertage.Add(new Feiertag { Name = "Reformationstag", Datum = new DateTime(jahr, 10, 31), Quelle = "Lokal" });
                break;

            case "HE": // Hessen
                feiertage.Add(new Feiertag { Name = "Fronleichnam", Datum = ostern.AddDays(60), Quelle = "Lokal" });
                break;

            case "NW": // Nordrhein-Westfalen
                feiertage.Add(new Feiertag { Name = "Fronleichnam", Datum = ostern.AddDays(60), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Allerheiligen", Datum = new DateTime(jahr, 11, 1), Quelle = "Lokal" });
                break;

            case "RP": // Rheinland-Pfalz
                feiertage.Add(new Feiertag { Name = "Fronleichnam", Datum = ostern.AddDays(60), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Allerheiligen", Datum = new DateTime(jahr, 11, 1), Quelle = "Lokal" });
                break;

            case "SL": // Saarland
                feiertage.Add(new Feiertag { Name = "Fronleichnam", Datum = ostern.AddDays(60), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Mariä Himmelfahrt", Datum = new DateTime(jahr, 8, 15), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Allerheiligen", Datum = new DateTime(jahr, 11, 1), Quelle = "Lokal" });
                break;

            case "SN": // Sachsen
                feiertage.Add(new Feiertag
                {
                    Name = "Fronleichnam",
                    Datum = ostern.AddDays(60),
                    Hinweis = "Nur in bestimmten Gemeinden",
                    Quelle = "Lokal"
                });
                feiertage.Add(new Feiertag { Name = "Reformationstag", Datum = new DateTime(jahr, 10, 31), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Buß- und Bettag", Datum = BerechnBussUndBettag(jahr), Quelle = "Lokal" });
                break;

            case "ST": // Sachsen-Anhalt
                feiertage.Add(new Feiertag { Name = "Heilige Drei Könige", Datum = new DateTime(jahr, 1, 6), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Reformationstag", Datum = new DateTime(jahr, 10, 31), Quelle = "Lokal" });
                break;

            case "TH": // Thüringen
                feiertage.Add(new Feiertag
                {
                    Name = "Fronleichnam",
                    Datum = ostern.AddDays(60),
                    Hinweis = "Nur in bestimmten Gemeinden",
                    Quelle = "Lokal"
                });
                feiertage.Add(new Feiertag { Name = "Weltkindertag", Datum = new DateTime(jahr, 9, 20), Quelle = "Lokal" });
                feiertage.Add(new Feiertag { Name = "Reformationstag", Datum = new DateTime(jahr, 10, 31), Quelle = "Lokal" });
                break;
        }

        return feiertage.OrderBy(f => f.Datum).ToList();
    }

    // ============================================================================
    // DATE CALCULATION HELPERS
    // ============================================================================

    /// <summary>
    /// Calculate Easter Sunday using Gauss algorithm
    /// </summary>
    private DateTime BerechnOstersonntag(int jahr)
    {
        int a = jahr % 19;
        int b = jahr / 100;
        int c = jahr % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int monat = (h + l - 7 * m + 114) / 31;
        int tag = ((h + l - 7 * m + 114) % 31) + 1;

        return new DateTime(jahr, monat, tag);
    }

    /// <summary>
    /// Calculate Buß- und Bettag (Wednesday before November 23)
    /// </summary>
    private DateTime BerechnBussUndBettag(int jahr)
    {
        var datum = new DateTime(jahr, 11, 23);

        while (datum.DayOfWeek != DayOfWeek.Wednesday)
        {
            datum = datum.AddDays(-1);
        }

        return datum;
    }

    // ============================================================================
    // HELPERS
    // ============================================================================

    private string BuildCacheKey(int jahr, string bundesland, string region)
    {
        var key = $"{jahr}-{bundesland}";
        if (!string.IsNullOrWhiteSpace(region) && region != "(Keine)")
        {
            key += $"-{region}";
        }
        return key;
    }

    /// <summary>
    /// Get list of all German states (Bundesländer)
    /// </summary>
    public static List<BundeslandInfo> GetAlleBundeslaender()
    {
        return new List<BundeslandInfo>
        {
            new() { Kuerzel = "BW", Name = "Baden-Württemberg", Regionen = new List<string>() },
            new() { Kuerzel = "BY", Name = "Bayern", Regionen = new List<string> { "Augsburg", "Überwiegend katholisch" } },
            new() { Kuerzel = "BE", Name = "Berlin", Regionen = new List<string>() },
            new() { Kuerzel = "BB", Name = "Brandenburg", Regionen = new List<string>() },
            new() { Kuerzel = "HB", Name = "Bremen", Regionen = new List<string>() },
            new() { Kuerzel = "HH", Name = "Hamburg", Regionen = new List<string>() },
            new() { Kuerzel = "HE", Name = "Hessen", Regionen = new List<string>() },
            new() { Kuerzel = "MV", Name = "Mecklenburg-Vorpommern", Regionen = new List<string>() },
            new() { Kuerzel = "NI", Name = "Niedersachsen", Regionen = new List<string>() },
            new() { Kuerzel = "NW", Name = "Nordrhein-Westfalen", Regionen = new List<string>() },
            new() { Kuerzel = "RP", Name = "Rheinland-Pfalz", Regionen = new List<string>() },
            new() { Kuerzel = "SL", Name = "Saarland", Regionen = new List<string>() },
            new() { Kuerzel = "SN", Name = "Sachsen", Regionen = new List<string>() },
            new() { Kuerzel = "ST", Name = "Sachsen-Anhalt", Regionen = new List<string>() },
            new() { Kuerzel = "SH", Name = "Schleswig-Holstein", Regionen = new List<string>() },
            new() { Kuerzel = "TH", Name = "Thüringen", Regionen = new List<string>() }
        };
    }
}

// ============================================================================
// DATA MODELS
// ============================================================================

/// <summary>
/// Holiday model
/// </summary>
public class Feiertag
{
    public string Name { get; set; } = string.Empty;
    public DateTime Datum { get; set; }
    public string Hinweis { get; set; } = string.Empty;
    public string Quelle { get; set; } = string.Empty; // "API", "Lokal", "Gespeichert"
}

/// <summary>
/// JSON serialization model
/// </summary>
internal class FeiertagJson
{
    public string Name { get; set; } = string.Empty;
    public string Datum { get; set; } = string.Empty; // Format: yyyy-MM-dd
    public string? Hinweis { get; set; }
    public string Quelle { get; set; } = string.Empty;
}

/// <summary>
/// API response model from feiertage-api.de
/// </summary>
internal class FeiertagApiResponse
{
    public string datum { get; set; } = string.Empty;
    public string? hinweis { get; set; }
}

/// <summary>
/// Info about saved holiday file
/// </summary>
public class FeiertagsDateiInfo
{
    public string Bundesland { get; set; } = string.Empty;
    public int Jahr { get; set; }
    public string Region { get; set; } = string.Empty;
    public string DateiName { get; set; } = string.Empty;
    public string Pfad { get; set; } = string.Empty;
}

/// <summary>
/// German state (Bundesland) info
/// </summary>
public class BundeslandInfo
{
    public string Kuerzel { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Regionen { get; set; } = new();
}
