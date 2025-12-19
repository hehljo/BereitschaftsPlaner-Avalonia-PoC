using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ExcelImportService _excelService = new();

    [ObservableProperty]
    private string _excelFilePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Ressource> _ressourcen = new();

    [ObservableProperty]
    private string _statusMessage = "Bereit";

    [ObservableProperty]
    private IBrush _statusColor = Brushes.Gray;

    [ObservableProperty]
    private bool _hasData = false;

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
                Ressourcen.Clear();
                foreach (var ressource in result.Ressourcen)
                {
                    Ressourcen.Add(ressource);
                }

                HasData = Ressourcen.Count > 0;
                OnPropertyChanged(nameof(DataGridTitle));

                SetStatus(result.Message, Brushes.Green);
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

                var result = _excelService.SaveToJson(Ressourcen.ToList(), jsonPath);

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

    private void SetStatus(string message, IBrush color)
    {
        StatusMessage = message;
        StatusColor = color;
    }
}
