using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BereitschaftsPlaner.Avalonia.ViewModels;
using BereitschaftsPlaner.Avalonia.Views;
using SettingsService = BereitschaftsPlaner.Avalonia.Services.Data.SettingsService;
using BereitschaftsPlaner.Avalonia.Services.Data;
using BereitschaftsPlaner.Avalonia.Services.Import;
using BereitschaftsPlaner.Avalonia.Services;
using Serilog;

namespace BereitschaftsPlaner.Avalonia;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    public static DatabaseService DatabaseService { get; private set; } = new();
    public static SettingsService SettingsService { get; private set; } = new();
    public static BackupService BackupService { get; private set; } = new();
    public static ZeitprofilService ZeitprofilService { get; private set; } = new(SettingsService);
    public static FeiertagsService FeiertagsService { get; private set; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            Log.Information("Framework initialization started");
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Initialize services and perform startup tasks (synchronous to avoid .Wait() deadlock)
                InitializeServices();

                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                desktop.MainWindow = MainWindow;

                Log.Information("Main window created and displayed");
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during framework initialization");
            throw;
        }
    }

    /// <summary>
    /// Initialize services on application startup (synchronous to avoid UI thread blocking)
    /// </summary>
    private void InitializeServices()
    {
        Log.Information("Initializing services...");

        // 1. Create backup before app starts (protects against update issues)
        BackupService.CreateBackupBeforeUpdate();

        // 2. Migrate settings if needed
        SettingsService.MigrateSettingsIfNeeded();

        // 3. Initialize default Zeitprofile if database is empty
        DatabaseService.InitializeDefaultZeitprofile();

        // 4. Check for PowerShell data to migrate (run in background to avoid blocking UI)
        var migrationService = new MigrationService(DatabaseService);
        if (migrationService.HasPowerShellDataToMigrate())
        {
            Log.Information("PowerShell data detected, starting background migration...");

            // Run migration in background without blocking UI thread
            Task.Run(async () =>
            {
                try
                {
                    await migrationService.MigrateFromPowerShellJsonAsync();
                    Log.Information("PowerShell data migration completed successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to migrate PowerShell data");
                }
            });
        }

        Log.Information("Services initialized successfully");
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
