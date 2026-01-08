using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly BereitschaftsPlaner.Avalonia.Services.ExcelImportService _excelService = new();
    private readonly DatabaseService _dbService;
    private readonly SettingsService _settingsService;

    // Navigation
    [ObservableProperty]
    private ViewModelBase? _currentView;

    public MainWindowViewModel()
    {
        _dbService = App.DatabaseService;
        _settingsService = App.SettingsService;

        // Initialize sub-ViewModels
        GeneratorVM = new GeneratorViewModel();
        EditorVM = new EditorViewModel();
        ZeitprofileVM = new ZeitprofileTabViewModel();

        // Load settings and apply theme
        LoadSettings();

        // Load existing data from database
        LoadDataFromDatabase();
    }

    // Sub-ViewModels for complex views
    public GeneratorViewModel GeneratorVM { get; }
    public EditorViewModel EditorVM { get; }
    public ZeitprofileTabViewModel ZeitprofileVM { get; }

    // Planning Board View (UserControl)
    public Views.PlanningBoardView PlanningBoardView { get; } = new();

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
    private async Task ImportExcel()
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

            if (!result.Success)
            {
                SetStatus(result.Message, Brushes.Red);
                HasData = false;
                return;
            }

            // Validate data
            var validator = new BereitschaftsPlaner.Avalonia.Services.DataValidator();
            var validationResult = validator.ValidateRessourcen(result.Ressourcen);

            // Clean data (remove duplicates, empty names)
            var cleanedData = validator.CleanRessourcen(result.Ressourcen);

            // Show preview dialog
            var previewVM = new ImportPreviewViewModel(
                cleanedData.Cast<object>().ToList(),
                "Ressourcen",
                validationResult
            );

            var previewWindow = new Views.ImportPreviewWindow(previewVM);

            // Show dialog and wait for user confirmation
            var confirmed = await previewWindow.ShowDialog<bool>(App.MainWindow!);

            if (!confirmed)
            {
                SetStatus("Import abgebrochen", Brushes.Orange);
                return;
            }

            // Save to database (only if user confirmed)
            _dbService.SaveRessourcen(cleanedData);

            // Update UI
            Ressourcen.Clear();
            foreach (var ressource in cleanedData)
            {
                Ressourcen.Add(ressource);
            }

            HasData = Ressourcen.Count > 0;
            OnPropertyChanged(nameof(DataGridTitle));

            // Update settings with last import path
            _settingsService.UpdateSetting<BereitschaftsPlaner.Avalonia.Models.AppSettings>(s =>
                s.LastImportPath = ExcelFilePath
            );

            // Clear file path after successful import
            ExcelFilePath = string.Empty;

            SetStatus($"✅ {cleanedData.Count} Ressourcen importiert und gespeichert", Brushes.Green);
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
    private async Task ImportGruppen()
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

            if (!result.Success)
            {
                SetStatus(result.Message, Brushes.Red);
                HasGruppenData = false;
                return;
            }

            // Validate data
            var validator = new BereitschaftsPlaner.Avalonia.Services.DataValidator();
            var validationResult = validator.ValidateBereitschaftsGruppen(result.Gruppen);

            // Clean data (remove duplicates, empty names)
            var cleanedData = validator.CleanBereitschaftsGruppen(result.Gruppen);

            // Show preview dialog
            var previewVM = new ImportPreviewViewModel(
                cleanedData.Cast<object>().ToList(),
                "Bereitschaftsgruppen",
                validationResult
            );

            var previewWindow = new Views.ImportPreviewWindow(previewVM);

            // Show dialog and wait for user confirmation
            var confirmed = await previewWindow.ShowDialog<bool>(App.MainWindow!);

            if (!confirmed)
            {
                SetStatus("Import abgebrochen", Brushes.Orange);
                return;
            }

            // Save to database (only if user confirmed)
            _dbService.SaveBereitschaftsGruppen(cleanedData);

            // Update UI
            BereitschaftsGruppen.Clear();
            foreach (var gruppe in cleanedData)
            {
                BereitschaftsGruppen.Add(gruppe);
            }

            HasGruppenData = BereitschaftsGruppen.Count > 0;

            // Clear file path after successful import
            GruppenFilePath = string.Empty;

            SetStatus($"✅ {cleanedData.Count} Bereitschaftsgruppen importiert und gespeichert", Brushes.Green);
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

    [RelayCommand]
    private async Task ResetDatabase()
    {
        // Ask for confirmation
        var confirmWindow = new Views.ConfirmDialog(
            "Datenbank zurücksetzen",
            "Möchten Sie wirklich alle importierten Daten löschen?\n\nEin Backup wird automatisch erstellt.",
            "Löschen",
            "Abbrechen"
        );

        var confirmed = await confirmWindow.ShowDialog<bool>(App.MainWindow!);

        if (!confirmed)
        {
            return;
        }

        try
        {
            SetStatus("Erstelle Backup...", Brushes.Blue);

            // Create backup before reset
            App.BackupService.CreateManualBackup();

            // Clear database
            _dbService.ClearAllData();

            // Clear UI
            Ressourcen.Clear();
            BereitschaftsGruppen.Clear();
            HasData = false;
            HasGruppenData = false;
            OnPropertyChanged(nameof(DataGridTitle));

            SetStatus("✅ Datenbank zurückgesetzt (Backup erstellt)", Brushes.Green);
        }
        catch (Exception ex)
        {
            SetStatus($"Fehler beim Zurücksetzen: {ex.Message}", Brushes.Red);
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
        _settingsService.UpdateSetting<BereitschaftsPlaner.Avalonia.Models.AppSettings>(s =>
            s.DarkModeEnabled = IsDarkMode
        );
    }

    /// <summary>
    /// Save environment preference to settings
    /// </summary>
    private void SaveEnvironmentSettings()
    {
        var environment = EnvironmentIndex == 1 ? "QA" : "Production";
        _settingsService.UpdateSetting<BereitschaftsPlaner.Avalonia.Models.AppSettings>(s =>
            s.Environment = environment
        );
    }

    // ============================================================================
    // BACKUP MANAGEMENT
    // ============================================================================

    /// <summary>
    /// Open Backup Management Window
    /// </summary>
    [RelayCommand]
    private async Task OpenBackupManagement()
    {
        var backupWindow = new Views.BackupManagementWindow();
        await backupWindow.ShowDialog(App.MainWindow!);
    }

    /// <summary>
    /// Open Settings Window
    /// </summary>
    [RelayCommand]
    private async Task OpenSettings()
    {
        var settingsWindow = new Views.SettingsWindow();
        await settingsWindow.ShowDialog(App.MainWindow!);
    }

    // ============================================================================
    // NAVIGATION (Sidebar)
    // ============================================================================

    [ObservableProperty]
    private int _selectedNavIndex = 0; // 0=Import, 1=Zeitprofile, 2=Generator, 3=Planning, 4=Editor

    // View visibility properties
    public bool IsImportView => SelectedNavIndex == 0;
    public bool IsZeitprofileView => SelectedNavIndex == 1;
    public bool IsGeneratorView => SelectedNavIndex == 2;
    public bool IsPlanningView => SelectedNavIndex == 3;
    public bool IsEditorView => SelectedNavIndex == 4;

    partial void OnSelectedNavIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsImportView));
        OnPropertyChanged(nameof(IsZeitprofileView));
        OnPropertyChanged(nameof(IsGeneratorView));
        OnPropertyChanged(nameof(IsPlanningView));
        OnPropertyChanged(nameof(IsEditorView));
    }

    [RelayCommand]
    private void NavigateToImport()
    {
        SelectedNavIndex = 0;
    }

    [RelayCommand]
    private void NavigateToZeitprofile()
    {
        SelectedNavIndex = 1;
    }

    [RelayCommand]
    private void NavigateToGenerator()
    {
        SelectedNavIndex = 2;
    }

    [RelayCommand]
    private void NavigateToPlanning()
    {
        SelectedNavIndex = 3;
    }

    [RelayCommand]
    private void NavigateToEditor()
    {
        SelectedNavIndex = 4;
    }
}
