using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using BereitschaftsPlaner.Avalonia.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class BackupManagementViewModel : ViewModelBase
{
    private readonly BackupService _backupService;
    private readonly Window _parentWindow;

    [ObservableProperty]
    private ObservableCollection<BackupInfo> _backups = new();

    [ObservableProperty]
    private BackupInfo? _selectedBackup;

    [ObservableProperty]
    private string _statusMessage = "Backups werden geladen...";

    [ObservableProperty]
    private double _totalBackupSizeMB;

    public BackupManagementViewModel(Window parentWindow)
    {
        _backupService = App.BackupService;
        _parentWindow = parentWindow;
        LoadBackups();
    }

    /// <summary>
    /// Load all available backups
    /// </summary>
    private void LoadBackups()
    {
        try
        {
            var backups = _backupService.GetAvailableBackups();
            Backups.Clear();

            foreach (var backup in backups)
            {
                Backups.Add(backup);
            }

            TotalBackupSizeMB = _backupService.GetTotalBackupSizeMB();

            StatusMessage = backups.Count == 0
                ? "Keine Backups vorhanden"
                : $"{backups.Count} Backup(s) gefunden ({TotalBackupSizeMB:F2} MB)";

            Log.Information("Loaded {Count} backups, total size {SizeMB:F2} MB", backups.Count, TotalBackupSizeMB);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Laden der Backups: {ex.Message}";
            Log.Error(ex, "Failed to load backups");
        }
    }

    /// <summary>
    /// Create a new manual backup
    /// </summary>
    [RelayCommand]
    private async Task CreateBackup()
    {
        try
        {
            StatusMessage = "Erstelle Backup...";

            var backupPath = _backupService.CreateManualBackup();

            if (backupPath != null)
            {
                StatusMessage = $"✅ Backup erstellt: {System.IO.Path.GetFileName(backupPath)}";
                Log.Information("Manual backup created at {BackupPath}", backupPath);

                // Reload backups
                await Task.Delay(100); // Small delay for file system
                LoadBackups();
            }
            else
            {
                StatusMessage = "❌ Backup konnte nicht erstellt werden (keine Datenbank vorhanden)";
                Log.Warning("Failed to create manual backup - no database found");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Fehler beim Erstellen: {ex.Message}";
            Log.Error(ex, "Failed to create manual backup");
        }
    }

    /// <summary>
    /// Restore from selected backup
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task RestoreBackup()
    {
        if (SelectedBackup == null) return;

        try
        {
            // Confirm with user
            var confirmDialog = new Views.ConfirmDialog(
                "Backup wiederherstellen",
                $"Möchten Sie wirklich das Backup vom {SelectedBackup.CreatedAt:dd.MM.yyyy HH:mm} wiederherstellen?\n\n" +
                "Die aktuelle Datenbank wird überschrieben!\n" +
                "Ein Sicherheits-Backup der aktuellen Datenbank wird automatisch erstellt.",
                "Wiederherstellen",
                "Abbrechen"
            );

            var confirmed = await confirmDialog.ShowDialog<bool>(_parentWindow);

            if (!confirmed) return;

            StatusMessage = "Stelle Backup wieder her...";

            var success = _backupService.RestoreFromBackup(SelectedBackup.FilePath);

            if (success)
            {
                StatusMessage = $"✅ Backup erfolgreich wiederhergestellt! Bitte App neu starten.";
                Log.Information("Successfully restored backup from {BackupPath}", SelectedBackup.FilePath);

                // Show restart dialog
                var restartDialog = new Views.ConfirmDialog(
                    "Neustart erforderlich",
                    "Das Backup wurde erfolgreich wiederhergestellt.\n\n" +
                    "Die Anwendung muss jetzt neu gestartet werden, um die Änderungen zu übernehmen.\n\n" +
                    "Anwendung jetzt beenden?",
                    "Beenden",
                    "Später"
                );

                var shouldExit = await restartDialog.ShowDialog<bool>(_parentWindow);

                if (shouldExit)
                {
                    System.Environment.Exit(0);
                }
            }
            else
            {
                StatusMessage = "❌ Wiederherstellung fehlgeschlagen";
                Log.Error("Failed to restore backup from {BackupPath}", SelectedBackup.FilePath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Fehler bei Wiederherstellung: {ex.Message}";
            Log.Error(ex, "Exception during backup restore");
        }
    }

    private bool CanRestore() => SelectedBackup != null;

    /// <summary>
    /// Delete selected backup
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteBackup()
    {
        if (SelectedBackup == null) return;

        try
        {
            // Confirm with user
            var confirmDialog = new Views.ConfirmDialog(
                "Backup löschen",
                $"Möchten Sie wirklich das Backup vom {SelectedBackup.CreatedAt:dd.MM.yyyy HH:mm} löschen?\n\n" +
                $"Größe: {SelectedBackup.SizeMB:F2} MB",
                "Löschen",
                "Abbrechen"
            );

            var confirmed = await confirmDialog.ShowDialog<bool>(_parentWindow);

            if (!confirmed) return;

            var success = _backupService.DeleteBackup(SelectedBackup.FilePath);

            if (success)
            {
                StatusMessage = $"✅ Backup gelöscht";
                Log.Information("Deleted backup {BackupPath}", SelectedBackup.FilePath);
                LoadBackups();
            }
            else
            {
                StatusMessage = "❌ Löschen fehlgeschlagen";
                Log.Warning("Failed to delete backup {BackupPath}", SelectedBackup.FilePath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Fehler beim Löschen: {ex.Message}";
            Log.Error(ex, "Exception during backup deletion");
        }
    }

    private bool CanDelete() => SelectedBackup != null;

    /// <summary>
    /// Refresh backup list
    /// </summary>
    [RelayCommand]
    private void Refresh()
    {
        LoadBackups();
    }

    /// <summary>
    /// Close the dialog
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _parentWindow.Close();
    }

    partial void OnSelectedBackupChanged(BackupInfo? value)
    {
        RestoreBackupCommand.NotifyCanExecuteChanged();
        DeleteBackupCommand.NotifyCanExecuteChanged();
    }
}
