using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BereitschaftsPlaner.Avalonia.Services.Data;

/// <summary>
/// Service for creating and managing database backups
/// Ensures no data loss during application updates
/// </summary>
public class BackupService
{
    private readonly string _appDataPath;
    private readonly string _backupPath;

    public BackupService()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BereitschaftsPlaner"
        );

        _backupPath = Path.Combine(_appDataPath, "backups");
        Directory.CreateDirectory(_backupPath);
    }

    /// <summary>
    /// Create a backup of the database before app update
    /// </summary>
    public string? CreateBackupBeforeUpdate()
    {
        var dbPath = Path.Combine(_appDataPath, "bereitschaftsplaner.db");

        if (!File.Exists(dbPath))
        {
            return null; // No database to backup
        }

        try
        {
            var version = GetAppVersion();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"bereitschaftsplaner_v{version}_{timestamp}.db";
            var backupFilePath = Path.Combine(_backupPath, backupFileName);

            File.Copy(dbPath, backupFilePath, overwrite: false);

            // Cleanup old backups (keep only last 10)
            CleanupOldBackups(maxBackups: 10);

            return backupFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Backup creation failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create a manual backup of the database
    /// </summary>
    public string? CreateManualBackup()
    {
        var dbPath = Path.Combine(_appDataPath, "bereitschaftsplaner.db");

        if (!File.Exists(dbPath))
        {
            return null;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"bereitschaftsplaner_manual_{timestamp}.db";
            var backupFilePath = Path.Combine(_backupPath, backupFileName);

            File.Copy(dbPath, backupFilePath, overwrite: false);

            return backupFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Manual backup failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Restore database from a backup file
    /// </summary>
    public bool RestoreFromBackup(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
        {
            return false;
        }

        try
        {
            var dbPath = Path.Combine(_appDataPath, "bereitschaftsplaner.db");

            // Create backup of current database before restoring
            if (File.Exists(dbPath))
            {
                var safeCopyPath = dbPath + ".before_restore.bak";
                File.Copy(dbPath, safeCopyPath, overwrite: true);
            }

            // Restore from backup
            File.Copy(backupFilePath, dbPath, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Restore failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get list of available backups
    /// </summary>
    public List<BackupInfo> GetAvailableBackups()
    {
        if (!Directory.Exists(_backupPath))
        {
            return new List<BackupInfo>();
        }

        var backupFiles = Directory.GetFiles(_backupPath, "*.db")
            .OrderByDescending(f => File.GetCreationTime(f))
            .Select(f => new BackupInfo
            {
                FilePath = f,
                FileName = Path.GetFileName(f),
                CreatedAt = File.GetCreationTime(f),
                SizeMB = new FileInfo(f).Length / (1024.0 * 1024.0)
            })
            .ToList();

        return backupFiles;
    }

    /// <summary>
    /// Delete old backups, keeping only the most recent N backups
    /// </summary>
    private void CleanupOldBackups(int maxBackups)
    {
        if (!Directory.Exists(_backupPath))
        {
            return;
        }

        var backupFiles = Directory.GetFiles(_backupPath, "*.db")
            .OrderByDescending(f => File.GetCreationTime(f))
            .Skip(maxBackups)
            .ToList();

        foreach (var oldBackup in backupFiles)
        {
            try
            {
                File.Delete(oldBackup);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete old backup {oldBackup}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Delete a specific backup file
    /// </summary>
    public bool DeleteBackup(string backupFilePath)
    {
        try
        {
            if (File.Exists(backupFilePath))
            {
                File.Delete(backupFilePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete backup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get total size of all backups in MB
    /// </summary>
    public double GetTotalBackupSizeMB()
    {
        if (!Directory.Exists(_backupPath))
        {
            return 0;
        }

        var totalBytes = Directory.GetFiles(_backupPath, "*.db")
            .Sum(f => new FileInfo(f).Length);

        return totalBytes / (1024.0 * 1024.0);
    }

    /// <summary>
    /// Get current application version
    /// </summary>
    private string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0"; // Major.Minor.Build
    }
}

/// <summary>
/// Information about a backup file
/// </summary>
public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public double SizeMB { get; set; }
}
