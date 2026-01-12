using System;
using BereitschaftsPlaner.Avalonia.Models;
using DataSettingsService = BereitschaftsPlaner.Avalonia.Services.Data.SettingsService;

namespace BereitschaftsPlaner.Avalonia.Services;

/// <summary>
/// Service for managing SBP (Service Business Platform) URLs
/// Provides environment-specific URLs for QA and Production
/// Uses user-configurable settings
/// </summary>
public class SBPUrlService
{
    private readonly DataSettingsService _settingsService;
    private readonly string _environment;

    public SBPUrlService(DataSettingsService settingsService, string environment)
    {
        _settingsService = settingsService;
        _environment = environment;
    }

    /// <summary>
    /// Get URL for Bookable Resources (Ressourcen) list
    /// Usage: Excel-Export für Ressourcen-Import
    /// </summary>
    public string GetBookableResourcesUrl()
    {
        var settings = _settingsService.GetSettings().SBPUrls;
        var baseUrl = settings.GetBaseUrl(_environment);
        var appId = settings.GetAppId(_environment);
        return $"{baseUrl}/main.aspx?appid={appId}&pagetype=entitylist&etn=bookableresource&viewid={settings.BookableResourceViewId}&viewType=1039";
    }

    /// <summary>
    /// Get URL for On-Call Groups (Bereitschaftsgruppen) list
    /// Usage: Excel-Export für Gruppen-Import
    /// </summary>
    public string GetOnCallGroupsUrl()
    {
        var settings = _settingsService.GetSettings().SBPUrls;
        var baseUrl = settings.GetBaseUrl(_environment);
        var appId = settings.GetAppId(_environment);
        return $"{baseUrl}/main.aspx?appid={appId}&newWindow=true&pagetype=entitylist&etn=sie_oncallgroup&viewid={settings.OnCallGroupViewId}&viewType=1039";
    }

    /// <summary>
    /// Get URL for My Imports (Meine Importe)
    /// Usage: Import-Status überwachen, Fehleranalyse
    /// </summary>
    public string GetMyImportsUrl()
    {
        var settings = _settingsService.GetSettings().SBPUrls;
        var baseUrl = settings.GetBaseUrl(_environment);
        var appId = settings.GetAppId(_environment);
        return $"{baseUrl}/main.aspx?appid={appId}&forceUCI=1&newWindow=true&pagetype=entitylist&etn=importfile&viewid={settings.ImportFileViewId}&viewType=1039";
    }

    /// <summary>
    /// Get URL for Import Overview (Alle Importe)
    /// Usage: Import nach Generierung überprüfen
    /// </summary>
    public string GetImportOverviewUrl()
    {
        var settings = _settingsService.GetSettings().SBPUrls;
        var baseUrl = settings.GetBaseUrl(_environment);
        var appId = settings.GetAppId(_environment);
        return $"{baseUrl}/main.aspx?appid={appId}&newWindow=true&pagetype=entitylist&etn=importfile";
    }

    /// <summary>
    /// Get URL for On-Call Duties (Bereitschaftsdienste)
    /// Usage: Prüfung nach Import, Übersicht aller Dienste
    /// </summary>
    public string GetOnCallDutiesUrl()
    {
        var settings = _settingsService.GetSettings().SBPUrls;
        var baseUrl = settings.GetBaseUrl(_environment);
        var appId = settings.GetAppId(_environment);
        return $"{baseUrl}/main.aspx?appid={appId}&forceUCI=1&newWindow=true&pagetype=entitylist&etn=sie_oncallduty&viewid={settings.OnCallDutyViewId}&viewType=1039";
    }

    /// <summary>
    /// Get URL for Data Management / Imports
    /// Usage: Neuen Import starten
    /// </summary>
    public string GetDataManagementUrl()
    {
        var settings = _settingsService.GetSettings().SBPUrls;
        var baseUrl = settings.GetBaseUrl(_environment);
        var appId = settings.GetAppId(_environment);
        return $"{baseUrl}/main.aspx?appid={appId}&pagetype=tools";
    }

    /// <summary>
    /// Get URL for Bookable Resource Bookings (for Template)
    /// Usage: Template erstellen - Export to Excel with "Make available for re-importing"
    /// </summary>
    public string GetBookableResourceBookingsUrl()
    {
        var settings = _settingsService.GetSettings().SBPUrls;
        var baseUrl = settings.GetBaseUrl(_environment);
        var appId = settings.GetAppId(_environment);
        return $"{baseUrl}/main.aspx?appid={appId}&pagetype=entitylist&etn=bookableresourcebooking";
    }

    /// <summary>
    /// Get current environment name
    /// </summary>
    public string GetEnvironmentName() => _environment;
}
