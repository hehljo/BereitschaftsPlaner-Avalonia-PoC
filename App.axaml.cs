using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BereitschaftsPlaner.Avalonia.ViewModels;
using BereitschaftsPlaner.Avalonia.Views;
using BereitschaftsPlaner.Avalonia.Services;
using BereitschaftsPlaner.Avalonia.Services.Data;
using BereitschaftsPlaner.Avalonia.Services.Import;

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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Initialize services and perform startup tasks
            InitializeServicesAsync().Wait();

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            desktop.MainWindow = MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Initialize services on application startup
    /// </summary>
    private async Task InitializeServicesAsync()
    {
        // 1. Create backup before app starts (protects against update issues)
        BackupService.CreateBackupBeforeUpdate();

        // 2. Migrate settings if needed
        SettingsService.MigrateSettingsIfNeeded();

        // 3. Initialize default Zeitprofile if database is empty
        DatabaseService.InitializeDefaultZeitprofile();

        // 4. Check for PowerShell data to migrate
        var migrationService = new MigrationService(DatabaseService);
        if (migrationService.HasPowerShellDataToMigrate())
        {
            // Automatically migrate on first run
            // (Could show dialog to user, but auto-migration is safer)
            await migrationService.MigrateFromPowerShellJsonAsync();
        }
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