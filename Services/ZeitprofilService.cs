using System;
using System.Collections.Generic;
using System.Linq;
using BereitschaftsPlaner.Avalonia.Models;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for managing Zeitprofile (time profiles) and group assignments
/// Matches PowerShell ZeitprofileManager.ps1 functionality
/// </summary>
public class ZeitprofilService
{
    private readonly BereitschaftsPlaner.Avalonia.Services.Data.SettingsService _settingsService;

    public ZeitprofilService(BereitschaftsPlaner.Avalonia.Services.Data.SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeZeitprofileStruktur();
    }

    /// <summary>
    /// Ensures Zeitprofile structure exists and creates Standard profile if needed
    /// </summary>
    private void InitializeZeitprofileStruktur()
    {
        var settings = _settingsService.LoadSettings();

        if (settings.Zeitprofile == null)
        {
            settings.Zeitprofile = new ZeitprofileSettings();
        }

        if (settings.Zeitprofile.Profile == null)
        {
            settings.Zeitprofile.Profile = new Dictionary<string, Zeitprofil>();
        }

        if (settings.Zeitprofile.GruppenZuweisungen == null)
        {
            settings.Zeitprofile.GruppenZuweisungen = new Dictionary<string, string>();
        }

        // Create Standard profile if it doesn't exist
        if (!settings.Zeitprofile.Profile.ContainsKey("Standard"))
        {
            var standardProfil = CreateStandardZeitprofil();
            settings.Zeitprofile.Profile["Standard"] = standardProfil;
            _settingsService.SaveSettings(settings);
        }
    }

    /// <summary>
    /// Creates a standard Zeitprofil with default values
    /// Matches PowerShell New-StandardZeitprofil function
    /// </summary>
    public Zeitprofil CreateStandardZeitprofil()
    {
        return new Zeitprofil
        {
            ProfilID = "Standard",
            Name = "Standard",
            BereitschaftsTage = new List<DienstTag>
            {
                new() { Tag = "Montag", Von = "16:00", Bis = "07:30" },
                new() { Tag = "Dienstag", Von = "16:00", Bis = "07:30" },
                new() { Tag = "Mittwoch", Von = "16:00", Bis = "07:30" },
                new() { Tag = "Donnerstag", Von = "16:00", Bis = "07:30" },
                new() { Tag = "Freitag", Von = "16:00", Bis = "07:30" },
                new() { Tag = "Samstag", Von = "16:00", Bis = "07:30" },
                new() { Tag = "Sonntag", Von = "16:00", Bis = "07:30" }
            },
            Tagesdienste = new List<DienstTag>
            {
                new() { Tag = "Montag", Von = "07:30", Bis = "16:00" },
                new() { Tag = "Dienstag", Von = "07:30", Bis = "16:00" },
                new() { Tag = "Mittwoch", Von = "07:30", Bis = "16:00" },
                new() { Tag = "Donnerstag", Von = "07:30", Bis = "16:00" },
                new() { Tag = "Freitag", Von = "07:30", Bis = "12:30" },
                new() { Tag = "Samstag", Von = "", Bis = "" },
                new() { Tag = "Sonntag", Von = "", Bis = "" }
            },
            Feiertage = new FeiertagsKonfiguration
            {
                Bundesland = "BY",
                Region = "",
                BehandelnWie = "Sonntag"
            },
            StandardTypFuerUndefiniert = "BD",
            // Backwards-compatible fields for database and legacy code
            StartZeit = "16:00",
            EndZeit = "07:30",
            Folgetag = true,
            Montag = true,
            Dienstag = true,
            Mittwoch = true,
            Donnerstag = true,
            Freitag = true,
            Samstag = true,
            Sonntag = true
        };
    }

    /// <summary>
    /// Gets all Zeitprofile
    /// Matches PowerShell Get-AlleZeitprofile function
    /// </summary>
    public Dictionary<string, Zeitprofil> GetAlleZeitprofile()
    {
        var settings = _settingsService.LoadSettings();
        return settings.Zeitprofile?.Profile ?? new Dictionary<string, Zeitprofil>();
    }

    /// <summary>
    /// Gets a specific Zeitprofil by ID
    /// Matches PowerShell Get-Zeitprofil function
    /// </summary>
    public Zeitprofil? GetZeitprofil(string profilID)
    {
        var settings = _settingsService.LoadSettings();
        if (settings.Zeitprofile?.Profile != null && settings.Zeitprofile.Profile.ContainsKey(profilID))
        {
            return settings.Zeitprofile.Profile[profilID];
        }
        return null;
    }

    /// <summary>
    /// Creates a new Zeitprofil
    /// Matches PowerShell New-Zeitprofil function
    /// </summary>
    public Zeitprofil CreateZeitprofil(string profilID, string name, string? vorlageProfil = null)
    {
        var settings = _settingsService.LoadSettings();

        if (settings.Zeitprofile.Profile.ContainsKey(profilID))
        {
            throw new Exception($"Profil mit ID '{profilID}' existiert bereits!");
        }

        Zeitprofil neuesProfil;

        if (!string.IsNullOrEmpty(vorlageProfil) && settings.Zeitprofile.Profile.ContainsKey(vorlageProfil))
        {
            // Copy from template
            var vorlage = settings.Zeitprofile.Profile[vorlageProfil];
            neuesProfil = CloneZeitprofil(vorlage);
            neuesProfil.ProfilID = profilID;
            neuesProfil.Name = name;
        }
        else
        {
            // Create new standard profile
            neuesProfil = CreateStandardZeitprofil();
            neuesProfil.ProfilID = profilID;
            neuesProfil.Name = name;
        }

        settings.Zeitprofile.Profile[profilID] = neuesProfil;
        _settingsService.SaveSettings(settings);

        return neuesProfil;
    }

    /// <summary>
    /// Updates an existing Zeitprofil
    /// Matches PowerShell Set-Zeitprofil function
    /// </summary>
    public void UpdateZeitprofil(string profilID, Zeitprofil profilDaten)
    {
        var settings = _settingsService.LoadSettings();

        if (!settings.Zeitprofile.Profile.ContainsKey(profilID))
        {
            throw new Exception($"Profil mit ID '{profilID}' existiert nicht!");
        }

        profilDaten.UpdatedAt = DateTime.Now;
        settings.Zeitprofile.Profile[profilID] = profilDaten;
        _settingsService.SaveSettings(settings);
    }

    /// <summary>
    /// Deletes a Zeitprofil
    /// Matches PowerShell Remove-Zeitprofil function
    /// </summary>
    public void DeleteZeitprofil(string profilID, bool force = false)
    {
        var settings = _settingsService.LoadSettings();

        if (!settings.Zeitprofile.Profile.ContainsKey(profilID))
        {
            throw new Exception($"Profil mit ID '{profilID}' existiert nicht!");
        }

        // Check if groups are assigned
        var zugewieseneGruppen = GetGruppenMitProfil(profilID);

        if (zugewieseneGruppen.Count > 0 && !force)
        {
            throw new Exception($"Profil wird noch von {zugewieseneGruppen.Count} Gruppe(n) verwendet: {string.Join(", ", zugewieseneGruppen)}");
        }

        // Remove assignments
        foreach (var gruppe in zugewieseneGruppen)
        {
            SetGruppenZeitprofil(gruppe, null);
        }

        // Delete profile
        settings.Zeitprofile.Profile.Remove(profilID);
        _settingsService.SaveSettings(settings);
    }

    /// <summary>
    /// Copies a Zeitprofil
    /// Matches PowerShell Copy-Zeitprofil function
    /// </summary>
    public Zeitprofil CopyZeitprofil(string quellProfilID, string neueProfilID, string neuerName)
    {
        var quellProfil = GetZeitprofil(quellProfilID);
        if (quellProfil == null)
        {
            throw new Exception($"Quell-Profil '{quellProfilID}' existiert nicht!");
        }

        return CreateZeitprofil(neueProfilID, neuerName, quellProfilID);
    }

    /// <summary>
    /// Assigns a Zeitprofil to a Bereitschaftsgruppe
    /// Matches PowerShell Set-GruppenZeitprofil function
    /// </summary>
    public void SetGruppenZeitprofil(string gruppenName, string? profilID)
    {
        var settings = _settingsService.LoadSettings();

        if (!string.IsNullOrEmpty(profilID) && !settings.Zeitprofile.Profile.ContainsKey(profilID))
        {
            throw new Exception($"Profil mit ID '{profilID}' existiert nicht!");
        }

        if (string.IsNullOrEmpty(profilID))
        {
            // Remove assignment (use Standard)
            settings.Zeitprofile.GruppenZuweisungen.Remove(gruppenName);
        }
        else
        {
            settings.Zeitprofile.GruppenZuweisungen[gruppenName] = profilID;
        }

        _settingsService.SaveSettings(settings);
    }

    /// <summary>
    /// Gets the Zeitprofil for a Bereitschaftsgruppe
    /// Matches PowerShell Get-GruppenZeitprofil function
    /// </summary>
    public (string ProfilID, string ProfilName, Zeitprofil ProfilDaten, bool IstStandard) GetGruppenZeitprofil(string gruppenName)
    {
        var settings = _settingsService.LoadSettings();
        string profilID = "Standard";

        if (settings.Zeitprofile.GruppenZuweisungen.ContainsKey(gruppenName))
        {
            profilID = settings.Zeitprofile.GruppenZuweisungen[gruppenName];
        }

        var profil = GetZeitprofil(profilID) ?? CreateStandardZeitprofil();

        return (profilID, profil.Name, profil, profilID == "Standard");
    }

    /// <summary>
    /// Gets all groups using a specific profile
    /// Matches PowerShell Get-GruppenMitProfil function
    /// </summary>
    public List<string> GetGruppenMitProfil(string profilID)
    {
        var settings = _settingsService.LoadSettings();
        var gruppen = new List<string>();

        foreach (var kvp in settings.Zeitprofile.GruppenZuweisungen)
        {
            if (kvp.Value == profilID)
            {
                gruppen.Add(kvp.Key);
            }
        }

        return gruppen;
    }

    /// <summary>
    /// Returns the profile ID assigned to a group, or null if none is set
    /// </summary>
    public string? GetProfilIDForGruppe(string gruppenName)
    {
        var settings = _settingsService.LoadSettings();
        if (settings.Zeitprofile != null && settings.Zeitprofile.GruppenZuweisungen != null && settings.Zeitprofile.GruppenZuweisungen.ContainsKey(gruppenName))
        {
            return settings.Zeitprofile.GruppenZuweisungen[gruppenName];
        }
        return null;
    }

    /// <summary>
    /// Clones a Zeitprofil (deep copy)
    /// </summary>
    private Zeitprofil CloneZeitprofil(Zeitprofil source)
    {
        return new Zeitprofil
        {
            ProfilID = source.ProfilID,
            Name = source.Name,
            BereitschaftsTage = source.BereitschaftsTage.Select(d => new DienstTag
            {
                Tag = d.Tag,
                Von = d.Von,
                Bis = d.Bis
            }).ToList(),
            Tagesdienste = source.Tagesdienste.Select(d => new DienstTag
            {
                Tag = d.Tag,
                Von = d.Von,
                Bis = d.Bis
            }).ToList(),
            Feiertage = new FeiertagsKonfiguration
            {
                Bundesland = source.Feiertage.Bundesland,
                Region = source.Feiertage.Region,
                BehandelnWie = source.Feiertage.BehandelnWie
            },
            StandardTypFuerUndefiniert = source.StandardTypFuerUndefiniert,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}
