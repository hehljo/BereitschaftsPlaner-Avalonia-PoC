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
            Serilog.Log.Debug("BrowseFile: Opening file picker for Ressourcen");

            // Get main window storage provider
            var mainWindow = App.MainWindow;
            if (mainWindow?.StorageProvider == null)
            {
                Serilog.Log.Error("BrowseFile: MainWindow StorageProvider is null");
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
                Serilog.Log.Information($"BrowseFile: File selected - {ExcelFilePath}");
                SetStatus($"Datei ausgewählt: {Path.GetFileName(ExcelFilePath)}", Brushes.Blue);
            }
            else
            {
                Serilog.Log.Debug("BrowseFile: No file selected (user cancelled)");
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "BrowseFile: Failed to open file picker");
            SetStatus($"Fehler bei Dateiauswahl: {ex.Message}", Brushes.Red);
        }
    }

    [RelayCommand]
    private async Task ImportExcel()
    {
        try
        {
            Serilog.Log.Debug("ImportExcel started");

            if (string.IsNullOrWhiteSpace(ExcelFilePath))
            {
                Serilog.Log.Warning("ImportExcel: No file path specified");
                SetStatus("Bitte zuerst eine Excel-Datei auswählen", Brushes.Orange);
                return;
            }

            Serilog.Log.Information($"ImportExcel: Importing from {ExcelFilePath}");
            SetStatus("Importiere Excel...", Brushes.Blue);

            var result = _excelService.ImportRessourcen(ExcelFilePath);
            Serilog.Log.Debug($"ImportExcel: ExcelService returned Success={result.Success}, Count={result.Ressourcen.Count}");

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

            // Show dialog
            Serilog.Log.Debug($"ImportExcel: Showing preview dialog for {cleanedData.Count} items");
            var confirmed = await previewWindow.ShowDialog<bool>(App.MainWindow!);

            if (!confirmed)
            {
                Serilog.Log.Information("ImportExcel: User cancelled import");
                SetStatus("Import abgebrochen", Brushes.Orange);
                return;
            }

            // Save to database (only if user confirmed)
            Serilog.Log.Information($"ImportExcel: Saving {cleanedData.Count} Ressourcen to database");
            _dbService.SaveRessourcen(cleanedData);

            // Update UI
            Serilog.Log.Debug("ImportExcel: Updating UI collections");
            Ressourcen.Clear();
            foreach (var ressource in cleanedData)
            {
                Ressourcen.Add(ressource);
            }

            HasData = Ressourcen.Count > 0;
            OnPropertyChanged(nameof(DataGridTitle));
            Serilog.Log.Debug($"ImportExcel: UI updated. HasData={HasData}, Ressourcen.Count={Ressourcen.Count}");

            // Update settings with last import path
            _settingsService.UpdateSetting<BereitschaftsPlaner.Avalonia.Models.AppSettings>(s =>
                s.LastImportPath = ExcelFilePath
            );

            // Clear file path after successful import
            ExcelFilePath = string.Empty;

            SetStatus($"✅ {cleanedData.Count} Ressourcen importiert und gespeichert", Brushes.Green);
            Serilog.Log.Information($"ImportExcel completed successfully: {cleanedData.Count} items");
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
            Serilog.Log.Debug("BrowseGruppenFile: Opening file picker for Gruppen");

            var mainWindow = App.MainWindow;
            if (mainWindow?.StorageProvider == null)
            {
                Serilog.Log.Error("BrowseGruppenFile: MainWindow StorageProvider is null");
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
                Serilog.Log.Information($"BrowseGruppenFile: File selected - {GruppenFilePath}");
                SetStatus($"Gruppendatei ausgewählt: {Path.GetFileName(GruppenFilePath)}", Brushes.Blue);
            }
            else
            {
                Serilog.Log.Debug("BrowseGruppenFile: No file selected (user cancelled)");
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "BrowseGruppenFile: Failed to open file picker");
            SetStatus($"Fehler bei Dateiauswahl: {ex.Message}", Brushes.Red);
        }
    }

    [RelayCommand]
    private async Task ImportGruppen()
    {
        try
        {
            Serilog.Log.Debug("ImportGruppen started");

            if (string.IsNullOrWhiteSpace(GruppenFilePath))
            {
                Serilog.Log.Warning("ImportGruppen: No file path specified");
                SetStatus("Bitte zuerst eine Excel-Datei für Gruppen auswählen", Brushes.Orange);
                return;
            }

            Serilog.Log.Information($"ImportGruppen: Importing from {GruppenFilePath}");
            SetStatus("Importiere Bereitschaftsgruppen...", Brushes.Blue);

            var result = _excelService.ImportBereitschaftsGruppen(GruppenFilePath);
            Serilog.Log.Debug($"ImportGruppen: ExcelService returned Success={result.Success}, Count={result.Gruppen.Count}");

            if (!result.Success)
            {
                Serilog.Log.Error($"ImportGruppen: Import failed - {result.Message}");
                SetStatus(result.Message, Brushes.Red);
                HasGruppenData = false;
                return;
            }

            // Validate data
            Serilog.Log.Debug("ImportGruppen: Validating data");
            var validator = new BereitschaftsPlaner.Avalonia.Services.DataValidator();
            var validationResult = validator.ValidateBereitschaftsGruppen(result.Gruppen);
            Serilog.Log.Debug($"ImportGruppen: Validation complete - IsValid={validationResult.IsValid}, Errors={validationResult.Errors.Count}");

            // Clean data (remove duplicates, empty names)
            var cleanedData = validator.CleanBereitschaftsGruppen(result.Gruppen);
            Serilog.Log.Debug($"ImportGruppen: Data cleaned - {cleanedData.Count} items after cleaning");

            // Show preview dialog
            var previewVM = new ImportPreviewViewModel(
                cleanedData.Cast<object>().ToList(),
                "Bereitschaftsgruppen",
                validationResult
            );

            var previewWindow = new Views.ImportPreviewWindow(previewVM);

            // Show dialog and wait for user confirmation
            Serilog.Log.Debug($"ImportGruppen: Showing preview dialog for {cleanedData.Count} items");
            var confirmed = await previewWindow.ShowDialog<bool>(App.MainWindow!);

            if (!confirmed)
            {
                Serilog.Log.Information("ImportGruppen: User cancelled import");
                SetStatus("Import abgebrochen", Brushes.Orange);
                return;
            }

            // Save to database (only if user confirmed)
            Serilog.Log.Information($"ImportGruppen: Saving {cleanedData.Count} Gruppen to database");
            _dbService.SaveBereitschaftsGruppen(cleanedData);

            // Update UI
            Serilog.Log.Debug("ImportGruppen: Updating UI collections");
            BereitschaftsGruppen.Clear();
            foreach (var gruppe in cleanedData)
            {
                BereitschaftsGruppen.Add(gruppe);
            }

            HasGruppenData = BereitschaftsGruppen.Count > 0;
            Serilog.Log.Debug($"ImportGruppen: UI updated. HasGruppenData={HasGruppenData}, BereitschaftsGruppen.Count={BereitschaftsGruppen.Count}");

            // Clear file path after successful import
            GruppenFilePath = string.Empty;

            SetStatus($"✅ {cleanedData.Count} Bereitschaftsgruppen importiert und gespeichert", Brushes.Green);
            Serilog.Log.Information($"ImportGruppen completed successfully: {cleanedData.Count} items");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "ImportGruppen failed");
            SetStatus($"Fehler beim Import: {ex.Message}", Brushes.Red);
            HasGruppenData = false;
        }
    }

    [RelayCommand]
    private async Task SaveJson()
    {
        try
        {
            Serilog.Log.Debug("SaveJson started");

            if (Ressourcen.Count == 0)
            {
                Serilog.Log.Warning("SaveJson: No data to save (Ressourcen.Count = 0)");
                SetStatus("Keine Daten zum Speichern vorhanden", Brushes.Orange);
                return;
            }

            // Get save location
            var mainWindow = App.MainWindow;
            if (mainWindow?.StorageProvider == null)
            {
                Serilog.Log.Error("SaveJson: MainWindow StorageProvider is null");
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
                Serilog.Log.Information($"SaveJson: Saving to {jsonPath}");
                SetStatus("Speichere JSON...", Brushes.Blue);

                // Save from database (always current data)
                var ressourcenFromDb = _dbService.GetAllRessourcen();
                Serilog.Log.Debug($"SaveJson: Got {ressourcenFromDb.Count} Ressourcen from database");

                var result = _excelService.SaveToJson(ressourcenFromDb, jsonPath);
                Serilog.Log.Debug($"SaveJson: ExcelService returned Success={result.Success}");

                if (result.Success)
                {
                    Serilog.Log.Information($"SaveJson: Successfully saved {ressourcenFromDb.Count} items to {jsonPath}");
                    SetStatus($"Erfolgreich gespeichert: {Path.GetFileName(jsonPath)}", Brushes.Green);
                }
                else
                {
                    Serilog.Log.Error($"SaveJson: Save failed - {result.Message}");
                    SetStatus(result.Message, Brushes.Red);
                }
            }
            else
            {
                Serilog.Log.Debug("SaveJson: User cancelled file save dialog");
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "SaveJson failed");
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
            Serilog.Log.Debug("LoadDataFromDatabase started");

            // Load Ressourcen
            var ressourcenFromDb = _dbService.GetAllRessourcen();
            Serilog.Log.Debug($"LoadDataFromDatabase: Got {ressourcenFromDb.Count} Ressourcen from database");

            if (ressourcenFromDb.Count > 0)
            {
                Ressourcen.Clear();
                foreach (var ressource in ressourcenFromDb)
                {
                    Ressourcen.Add(ressource);
                }
                HasData = true;
                OnPropertyChanged(nameof(DataGridTitle));
                Serilog.Log.Information($"LoadDataFromDatabase: Loaded {Ressourcen.Count} Ressourcen into UI");
            }
            else
            {
                Serilog.Log.Information("LoadDataFromDatabase: No Ressourcen found in database");
            }

            // Load Bereitschaftsgruppen
            var gruppenFromDb = _dbService.GetAllBereitschaftsGruppen();
            Serilog.Log.Debug($"LoadDataFromDatabase: Got {gruppenFromDb.Count} Gruppen from database");

            if (gruppenFromDb.Count > 0)
            {
                BereitschaftsGruppen.Clear();
                foreach (var gruppe in gruppenFromDb)
                {
                    BereitschaftsGruppen.Add(gruppe);
                }
                HasGruppenData = true;
                Serilog.Log.Information($"LoadDataFromDatabase: Loaded {BereitschaftsGruppen.Count} Gruppen into UI");
            }
            else
            {
                Serilog.Log.Information("LoadDataFromDatabase: No Gruppen found in database");
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
        Serilog.Log.Debug("ResetDatabase: Showing confirmation dialog");

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
            Serilog.Log.Information("ResetDatabase: User cancelled reset");
            return;
        }

        try
        {
            Serilog.Log.Information("ResetDatabase: User confirmed - starting reset process");
            SetStatus("Erstelle Backup...", Brushes.Blue);

            // Create backup before reset
            Serilog.Log.Debug("ResetDatabase: Creating backup");
            App.BackupService.CreateManualBackup();
            Serilog.Log.Information("ResetDatabase: Backup created successfully");

            // Clear database
            Serilog.Log.Debug("ResetDatabase: Clearing database");
            _dbService.ClearAllData();
            Serilog.Log.Information("ResetDatabase: Database cleared");

            // Clear UI
            Serilog.Log.Debug("ResetDatabase: Clearing UI collections");
            Ressourcen.Clear();
            BereitschaftsGruppen.Clear();
            HasData = false;
            HasGruppenData = false;
            OnPropertyChanged(nameof(DataGridTitle));
            Serilog.Log.Debug("ResetDatabase: UI cleared");

            SetStatus("✅ Datenbank zurückgesetzt (Backup erstellt)", Brushes.Green);
            Serilog.Log.Information("ResetDatabase completed successfully");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "ResetDatabase failed");
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
