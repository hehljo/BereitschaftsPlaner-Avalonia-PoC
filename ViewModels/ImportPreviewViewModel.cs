using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using BereitschaftsPlaner.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class ImportPreviewViewModel : ViewModelBase
{
    public event Action<bool>? DialogResultRequested;

    private ObservableCollection<object> _previewData = new();

    public ObservableCollection<object> PreviewData
    {
        get
        {
            if (DebugConfig.IsEnabled(DebugConfig.ImportPreview))
                Serilog.Log.Debug($"[IMPORT PREVIEW] ImportPreviewViewModel.PreviewData GET: Count={_previewData.Count}");
            return _previewData;
        }
        set
        {
            if (DebugConfig.IsEnabled(DebugConfig.ImportPreview))
                Serilog.Log.Debug($"[IMPORT PREVIEW] ImportPreviewViewModel.PreviewData SET: NewCount={value?.Count ?? 0}");
            SetProperty(ref _previewData, value ?? new());
        }
    }

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Color _statusColor = Colors.Green;

    [ObservableProperty]
    private bool _hasErrors = false;

    [ObservableProperty]
    private string _errorDetails = string.Empty;

    [ObservableProperty]
    private bool _canImport = true;

    [ObservableProperty]
    private string _title = string.Empty;

    public ImportPreviewViewModel()
    {
        // Design-time data
    }

    public ImportPreviewViewModel(
        List<object> data,
        string title,
        ValidationResult? validationResult = null)
    {
        Title = title;

        Serilog.Log.Debug($"ImportPreviewViewModel: Creating with {data.Count} items, Title='{title}'");

        // Add data to collection
        foreach (var item in data)
        {
            PreviewData.Add(item);
            Serilog.Log.Debug($"ImportPreviewViewModel: Added item of type {item.GetType().Name}");
        }

        Serilog.Log.Debug($"ImportPreviewViewModel: PreviewData.Count = {PreviewData.Count}");

        // Force property changed notification to ensure DataGrid refreshes
        OnPropertyChanged(nameof(PreviewData));
        Serilog.Log.Debug("ImportPreviewViewModel: PropertyChanged notification sent for PreviewData");

        // Set status based on validation
        if (validationResult != null)
        {
            if (!validationResult.IsValid)
            {
                StatusMessage = $"⚠ WARNUNG: {validationResult.Errors.Count} Fehler gefunden!";
                StatusColor = Colors.Red;
                HasErrors = true;
                ErrorDetails = string.Join("\n", validationResult.Errors);

                // Disable import if no valid data
                if (data.Count == 0)
                {
                    CanImport = false;
                    StatusMessage = "❌ Keine importierbaren Daten gefunden";
                }
            }
            else if (validationResult.WarningCount > 0)
            {
                StatusMessage = $"⚠ HINWEIS: {validationResult.WarningCount} Einträge werden übersprungen";
                StatusColor = Color.Parse("#FFA500"); // Orange
            }
            else
            {
                StatusMessage = $"✓ {data.Count} Einträge bereit zum Importieren";
                StatusColor = Colors.Green;
            }
        }
        else
        {
            StatusMessage = $"✓ {data.Count} Einträge bereit zum Importieren";
            StatusColor = Colors.Green;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        DialogResultRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResultRequested?.Invoke(false);
    }
}

/// <summary>
/// Validation result for import data
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public int WarningCount { get; set; } = 0;
}
