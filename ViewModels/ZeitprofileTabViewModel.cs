using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services;
using BereitschaftsPlaner.Avalonia.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class ZeitprofileTabViewModel : ViewModelBase
{
    private readonly ZeitprofilService _zeitprofilService;
    private readonly DatabaseService _dbService;

    public ZeitprofileTabViewModel()
    {
        _zeitprofilService = App.ZeitprofilService;
        _dbService = App.DatabaseService;

        LoadZeitprofile();
    }

    [ObservableProperty]
    private ObservableCollection<ZeitprofilViewModel> _zeitprofile = new();

    [ObservableProperty]
    private ZeitprofilViewModel? _selectedZeitprofil;

    [ObservableProperty]
    private string _statusMessage = "Bereit";

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private bool _isEditing = false;

    [ObservableProperty]
    private string _newProfilName = string.Empty;

    /// <summary>
    /// Loads all Zeitprofile from settings
    /// </summary>
    private void LoadZeitprofile()
    {
        try
        {
            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Debug($"[ZEITPROFILE] Loading all Zeitprofile...");

            var profile = _zeitprofilService.GetAlleZeitprofile();
            Zeitprofile.Clear();

            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Debug($"[ZEITPROFILE] Found {profile.Count} profiles in database");

            foreach (var kvp in profile.OrderBy(p => p.Key))
            {
                var vm = new ZeitprofilViewModel(kvp.Value, _zeitprofilService, _dbService);
                Zeitprofile.Add(vm);

                if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                    Serilog.Log.Debug($"[ZEITPROFILE] Loaded profile: {kvp.Key} - {kvp.Value.Name}, BD: {vm.BereitschaftsTage.Count}, TD: {vm.Tagesdienste.Count}, Groups: {vm.GruppenZuweisungen.Count}");
            }

            // Select Standard profile by default
            SelectedZeitprofil = Zeitprofile.FirstOrDefault(p => p.ProfilID == "Standard");

            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Debug($"[ZEITPROFILE] Selected default profile: {SelectedZeitprofil?.Name ?? "null"}");

            SetStatus($"{Zeitprofile.Count} Zeitprofile geladen", Brushes.Green);
        }
        catch (Exception ex)
        {
            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Error(ex, $"[ZEITPROFILE] Error loading profiles: {ex.Message}");
            SetStatus($"Fehler beim Laden: {ex.Message}", Brushes.Red);
        }
    }

    /// <summary>
    /// Creates a new Zeitprofil
    /// </summary>
    [RelayCommand]
    private void CreateNewProfil()
    {
        if (string.IsNullOrWhiteSpace(NewProfilName))
        {
            SetStatus("Bitte einen Namen eingeben", Brushes.Orange);
            return;
        }

        try
        {
            var profilID = NewProfilName.Replace(" ", "_");
            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Debug($"[ZEITPROFILE] Creating new profile: Name='{NewProfilName}', ProfilID='{profilID}'");

            var neuesProfil = _zeitprofilService.CreateZeitprofil(profilID, NewProfilName);

            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Debug($"[ZEITPROFILE] Profile created successfully. BD entries: {neuesProfil.BereitschaftsTage.Count}, TD entries: {neuesProfil.Tagesdienste.Count}");

            LoadZeitprofile();
            SelectedZeitprofil = Zeitprofile.FirstOrDefault(p => p.ProfilID == profilID);

            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Debug($"[ZEITPROFILE] Profile selected. Total profiles: {Zeitprofile.Count}, Selected: {SelectedZeitprofil?.Name ?? "null"}");

            NewProfilName = string.Empty;
            SetStatus($"Profil '{neuesProfil.Name}' erstellt", Brushes.Green);
        }
        catch (Exception ex)
        {
            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Error(ex, $"[ZEITPROFILE] Error creating profile: {ex.Message}");
            SetStatus($"Fehler: {ex.Message}", Brushes.Red);
        }
    }

    /// <summary>
    /// Copies the selected Zeitprofil
    /// </summary>
    [RelayCommand]
    private void CopySelectedProfil()
    {
        if (SelectedZeitprofil == null)
        {
            SetStatus("Bitte ein Profil auswählen", Brushes.Orange);
            return;
        }

        try
        {
            var neuerName = $"{SelectedZeitprofil.Name} (Kopie)";
            var neueProfilID = $"{SelectedZeitprofil.ProfilID}_Kopie";

            var kopie = _zeitprofilService.CopyZeitprofil(SelectedZeitprofil.ProfilID, neueProfilID, neuerName);

            LoadZeitprofile();
            SelectedZeitprofil = Zeitprofile.FirstOrDefault(p => p.ProfilID == neueProfilID);

            SetStatus($"Profil kopiert: '{kopie.Name}'", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler: {ex.Message}", Brushes.Red);
        }
    }

    /// <summary>
    /// Deletes the selected Zeitprofil
    /// </summary>
    [RelayCommand]
    private void DeleteSelectedProfil()
    {
        if (SelectedZeitprofil == null)
        {
            SetStatus("Bitte ein Profil auswählen", Brushes.Orange);
            return;
        }

        if (SelectedZeitprofil.ProfilID == "Standard")
        {
            SetStatus("Standard-Profil kann nicht gelöscht werden", Brushes.Orange);
            return;
        }

        try
        {
            _zeitprofilService.DeleteZeitprofil(SelectedZeitprofil.ProfilID);

            LoadZeitprofile();
            SelectedZeitprofil = Zeitprofile.FirstOrDefault(p => p.ProfilID == "Standard");

            SetStatus("Profil gelöscht", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler: {ex.Message}", Brushes.Red);
        }
    }

    /// <summary>
    /// Saves the currently selected Zeitprofil
    /// </summary>
    [RelayCommand]
    private void SaveSelectedProfil()
    {
        if (SelectedZeitprofil == null)
        {
            SetStatus("Bitte ein Profil auswählen", Brushes.Orange);
            return;
        }

        try
        {
            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Debug($"[ZEITPROFILE] Saving profile: {SelectedZeitprofil.ProfilID} - {SelectedZeitprofil.Name}, BD: {SelectedZeitprofil.BereitschaftsTage.Count}, TD: {SelectedZeitprofil.Tagesdienste.Count}");

            SelectedZeitprofil.Save();

            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Debug($"[ZEITPROFILE] Profile saved successfully");

            SetStatus($"Profil '{SelectedZeitprofil.Name}' gespeichert", Brushes.Green);
        }
        catch (Exception ex)
        {
            if (DebugConfig.IsEnabled(DebugConfig.Zeitprofile))
                Serilog.Log.Error(ex, $"[ZEITPROFILE] Error saving profile: {ex.Message}");
            SetStatus($"Fehler: {ex.Message}", Brushes.Red);
        }
    }

    private void SetStatus(string message, IBrush color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}

/// <summary>
/// ViewModel for a single Zeitprofil with edit capabilities
/// </summary>
public partial class ZeitprofilViewModel : ViewModelBase
{
    private readonly ZeitprofilService _zeitprofilService;
    private readonly DatabaseService _dbService;
    private readonly Zeitprofil _zeitprofil;

    public ZeitprofilViewModel(Zeitprofil zeitprofil, ZeitprofilService zeitprofilService, DatabaseService dbService)
    {
        _zeitprofil = zeitprofil;
        _zeitprofilService = zeitprofilService;
        _dbService = dbService;

        // Load day configurations
        LoadBereitschaftsTage();
        LoadTagesdienste();
        LoadFeiertagsKonfiguration();
        LoadGruppenZuweisungen();
    }

    public string ProfilID => _zeitprofil.ProfilID;

    [ObservableProperty]
    private string _name = string.Empty;

    public override string ToString() => Name;

    [ObservableProperty]
    private ObservableCollection<DienstTagViewModel> _bereitschaftsTage = new();

    [ObservableProperty]
    private ObservableCollection<DienstTagViewModel> _tagesdienste = new();

    [ObservableProperty]
    private string _bundesland = "BY";

    [ObservableProperty]
    private string _region = string.Empty;

    [ObservableProperty]
    private string _behandelnWie = "Sonntag";

    [ObservableProperty]
    private ObservableCollection<GruppenZuweisungViewModel> _gruppenZuweisungen = new();

    private void LoadBereitschaftsTage()
    {
        Name = _zeitprofil.Name;
        BereitschaftsTage.Clear();
        foreach (var tag in _zeitprofil.BereitschaftsTage)
        {
            BereitschaftsTage.Add(new DienstTagViewModel(tag));
        }
    }

    private void LoadTagesdienste()
    {
        Tagesdienste.Clear();
        foreach (var tag in _zeitprofil.Tagesdienste)
        {
            Tagesdienste.Add(new DienstTagViewModel(tag));
        }
    }

    private void LoadFeiertagsKonfiguration()
    {
        Bundesland = _zeitprofil.Feiertage.Bundesland;
        Region = _zeitprofil.Feiertage.Region;
        BehandelnWie = _zeitprofil.Feiertage.BehandelnWie;
    }

    private void LoadGruppenZuweisungen()
    {
        GruppenZuweisungen.Clear();
        var gruppen = _zeitprofilService.GetGruppenMitProfil(ProfilID);

        foreach (var gruppe in gruppen)
        {
            GruppenZuweisungen.Add(new GruppenZuweisungViewModel
            {
                GruppenName = gruppe,
                ProfilID = ProfilID,
                ProfilName = Name
            });
        }
    }

    /// <summary>
    /// Saves changes to this Zeitprofil
    /// </summary>
    public void Save()
    {
        _zeitprofil.Name = Name;

        // Update BD
        _zeitprofil.BereitschaftsTage.Clear();
        foreach (var tag in BereitschaftsTage)
        {
            _zeitprofil.BereitschaftsTage.Add(new DienstTag
            {
                Tag = tag.Tag,
                Von = tag.Von,
                Bis = tag.Bis
            });
        }

        // Update TD
        _zeitprofil.Tagesdienste.Clear();
        foreach (var tag in Tagesdienste)
        {
            _zeitprofil.Tagesdienste.Add(new DienstTag
            {
                Tag = tag.Tag,
                Von = tag.Von,
                Bis = tag.Bis
            });
        }

        // Update Feiertage
        _zeitprofil.Feiertage.Bundesland = Bundesland;
        _zeitprofil.Feiertage.Region = Region;
        _zeitprofil.Feiertage.BehandelnWie = BehandelnWie;

        _zeitprofilService.UpdateZeitprofil(ProfilID, _zeitprofil);
    }
}

/// <summary>
/// ViewModel for a single DienstTag (editable)
/// </summary>
public partial class DienstTagViewModel : ViewModelBase
{
    public DienstTagViewModel(DienstTag tag)
    {
        Tag = tag.Tag;
        Von = tag.Von;
        Bis = tag.Bis;
    }

    [ObservableProperty]
    private string _tag = string.Empty;

    [ObservableProperty]
    private string _von = string.Empty;

    [ObservableProperty]
    private string _bis = string.Empty;
}

/// <summary>
/// ViewModel for Gruppen assignment display
/// </summary>
public partial class GruppenZuweisungViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _gruppenName = string.Empty;

    [ObservableProperty]
    private string _profilID = string.Empty;

    [ObservableProperty]
    private string _profilName = string.Empty;
}
