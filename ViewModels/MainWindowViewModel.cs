using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services;
using BereitschaftsPlaner.Avalonia.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ExcelImportService _excelService = new();
    private readonly DatabaseService _dbService;
    private readonly SettingsService _settingsService;

    public MainWindowViewModel()
    {
        _dbService = App.DatabaseService;
        _settingsService = App.SettingsService;

        // Load settings and apply theme
        LoadSettings();

        // Load existing data from database
        LoadDataFromDatabase();
    }

    [ObservableProperty]
    private string _excelFilePath = string.Empty;

    [ObservableProperty]
    private string _gruppenFilePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Ressource> _ressourcen = new();

    [ObservableProperty]
    private ObservableCollection<BereitschaftsGruppe> _bereitschaftsGruppen = new();

    [ObservableProperty]
    private string _statusMessage = "Bereit";

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private bool _hasData = false;

    [ObservableProperty]
    private bool _hasGruppenData = false;

    [ObservableProperty]
    private bool _isDarkMode = false;

    [ObservableProperty]
    private int _environmentIndex = 0; // 0 = Production, 1 = QA

    public string DataGridTitle => HasData
        ? $"Importierte Ressourcen ({Ressourcen.Count} Einträge)"
        : "Importierte Ressourcen (0 Einträge)";

    [RelayCommand]
    private async Task BrowseFile()
    {
        try
        {
            // Get main window storage provider
            var mainWindow = App.MainWindow;
            if (mainWindow?.StorageProvider == null)
            {
                SetStatus("Fehler: Hauptfenster nicht verfügbar", Brushes.Red);
                return;
            }

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Excel-Datei auswählen",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Excel Dateien") { Patterns = new[] { "*.xlsx", "*.xls" } },
                    new FilePickerFileType("Alle Dateien") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                ExcelFilePath = files[0].Path.LocalPath;
                SetStatus($"Datei ausgewählt: {Path.GetFileName(ExcelFilePath)}", Brushes.Blue);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler bei Dateiauswahl: {ex.Message}", Brushes.Red);
        }
    }

    [RelayCommand]
    private void ImportExcel()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ExcelFilePath))
            {
                SetStatus("Bitte zuerst eine Excel-Datei auswählen", Brushes.Orange);
                return;
            }

            SetStatus("Importiere Excel...", Brushes.Blue);

            var result = _excelService.ImportRessourcen(ExcelFilePath);

            if (result.Success)
            {
                // Save to database
                _dbService.SaveRessourcen(result.Ressourcen);

                // Update UI
                Ressourcen.Clear();
                foreach (var ressource in result.Ressourcen)
                {
                    Ressourcen.Add(ressource);
                }

                HasData = Ressourcen.Count > 0;
                OnPropertyChanged(nameof(DataGridTitle));

                // Update settings with last import path
                _settingsService.UpdateSetting<Models.AppSettings>(s =>
                    s.LastImportPath = ExcelFilePath
                );

                // Clear file path after successful import
                ExcelFilePath = string.Empty;

                SetStatus($"{result.Message} (in Datenbank gespeichert)", Brushes.Green);
            }
            else
            {
                SetStatus(result.Message, Brushes.Red);
                HasData = false;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Import: {ex.Message}", Brushes.Red);
            HasData = false;
        }
    }

    [RelayCommand]
    private async Task BrowseGruppenFile()
    {
        try
        {
            var mainWindow = App.MainWindow;
            if (mainWindow?.StorageProvider == null)
            {
                SetStatus("Fehler: Hauptfenster nicht verfügbar", Brushes.Red);
                return;
            }

            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Bereitschaftsgruppen Excel-Datei auswählen",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Excel Dateien") { Patterns = new[] { "*.xlsx", "*.xls" } },
                    new FilePickerFileType("Alle Dateien") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                GruppenFilePath = files[0].Path.LocalPath;
                SetStatus($"Gruppendatei ausgewählt: {Path.GetFileName(GruppenFilePath)}", Brushes.Blue);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler bei Dateiauswahl: {ex.Message}", Brushes.Red);
        }
    }

    [RelayCommand]
    private void ImportGruppen()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(GruppenFilePath))
            {
                SetStatus("Bitte zuerst eine Excel-Datei für Gruppen auswählen", Brushes.Orange);
                return;
            }

            SetStatus("Importiere Bereitschaftsgruppen...", Brushes.Blue);

            var result = _excelService.ImportBereitschaftsGruppen(GruppenFilePath);

            if (result.Success)
            {
                // Save to database
                _dbService.SaveBereitschaftsGruppen(result.Gruppen);

                // Update UI
                BereitschaftsGruppen.Clear();
                foreach (var gruppe in result.Gruppen)
                {
                    BereitschaftsGruppen.Add(gruppe);
                }

                HasGruppenData = BereitschaftsGruppen.Count > 0;

                // Clear file path after successful import
                GruppenFilePath = string.Empty;

                SetStatus($"{result.Message} (in Datenbank gespeichert)", Brushes.Green);
            }
            else
            {
                SetStatus(result.Message, Brushes.Red);
                HasGruppenData = false;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Import: {ex.Message}", Brushes.Red);
            HasGruppenData = false;
        }
    }

    [RelayCommand]
    private async Task SaveJson()
    {
        try
        {
            if (Ressourcen.Count == 0)
            {
                SetStatus("Keine Daten zum Speichern vorhanden", Brushes.Orange);
                return;
            }

            // Get save location
            var mainWindow = App.MainWindow;
            if (mainWindow?.StorageProvider == null)
            {
                SetStatus("Fehler: Hauptfenster nicht verfügbar", Brushes.Red);
                return;
            }

            var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "JSON-Datei speichern",
                DefaultExtension = "json",
                SuggestedFileName = "ressourcen.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON Dateien") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("Alle Dateien") { Patterns = new[] { "*.*" } }
                }
            });

            if (file != null)
            {
                var jsonPath = file.Path.LocalPath;
                SetStatus("Speichere JSON...", Brushes.Blue);

                // Save from database (always current data)
                var ressourcenFromDb = _dbService.GetAllRessourcen();
                var result = _excelService.SaveToJson(ressourcenFromDb, jsonPath);

                if (result.Success)
                {
                    SetStatus($"Erfolgreich gespeichert: {Path.GetFileName(jsonPath)}", Brushes.Green);
                }
                else
                {
                    SetStatus(result.Message, Brushes.Red);
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Speichern: {ex.Message}", Brushes.Red);
        }
    }

    /// <summary>
    /// Load existing data from database on startup
    /// </summary>
    private void LoadDataFromDatabase()
    {
        try
        {
            // Load Ressourcen
            var ressourcenFromDb = _dbService.GetAllRessourcen();
            if (ressourcenFromDb.Count > 0)
            {
                Ressourcen.Clear();
                foreach (var ressource in ressourcenFromDb)
                {
                    Ressourcen.Add(ressource);
                }
                HasData = true;
                OnPropertyChanged(nameof(DataGridTitle));
            }

            // Load Bereitschaftsgruppen
            var gruppenFromDb = _dbService.GetAllBereitschaftsGruppen();
            if (gruppenFromDb.Count > 0)
            {
                BereitschaftsGruppen.Clear();
                foreach (var gruppe in gruppenFromDb)
                {
                    BereitschaftsGruppen.Add(gruppe);
                }
                HasGruppenData = true;
            }

            // Status message
            if (ressourcenFromDb.Count > 0 || gruppenFromDb.Count > 0)
            {
                SetStatus($"{ressourcenFromDb.Count} Ressourcen, {gruppenFromDb.Count} Gruppen aus Datenbank geladen", Brushes.Green);
            }
            else
            {
                SetStatus("Bereit - Keine Daten in Datenbank", Brushes.Gray);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Laden der Daten: {ex.Message}", Brushes.Red);
        }
    }

    private void SetStatus(string message, IBrush color)
    {
        StatusMessage = message;
        StatusColor = color;
    }

    /// <summary>
    /// Load settings (theme, environment)
    /// </summary>
    private void LoadSettings()
    {
        var settings = _settingsService.LoadSettings();
        IsDarkMode = settings.DarkModeEnabled;
        EnvironmentIndex = settings.Environment == "QA" ? 1 : 0;

        // Apply theme
        ApplyTheme(IsDarkMode);
    }

    /// <summary>
    /// Called when Dark Mode toggle changes
    /// </summary>
    partial void OnIsDarkModeChanged(bool value)
    {
        ApplyTheme(value);
        SaveThemeSettings();
    }

    /// <summary>
    /// Called when Environment selection changes
    /// </summary>
    partial void OnEnvironmentIndexChanged(int value)
    {
        SaveEnvironmentSettings();
    }

    /// <summary>
    /// Apply Dark/Light theme
    /// </summary>
    private void ApplyTheme(bool isDark)
    {
        if (App.MainWindow != null)
        {
            var themeVariant = isDark ? global::Avalonia.Styling.ThemeVariant.Dark : global::Avalonia.Styling.ThemeVariant.Light;
            App.MainWindow.RequestedThemeVariant = themeVariant;
        }
    }

    /// <summary>
    /// Save theme preference to settings
    /// </summary>
    private void SaveThemeSettings()
    {
        _settingsService.UpdateSetting<AppSettings>(s =>
            s.DarkModeEnabled = IsDarkMode
        );
    }

    /// <summary>
    /// Save environment preference to settings
    /// </summary>
    private void SaveEnvironmentSettings()
    {
        var environment = EnvironmentIndex == 1 ? "QA" : "Production";
        _settingsService.UpdateSetting<AppSettings>(s =>
            s.Environment = environment
        );
    }
}
