# CLAUDE.md - BereitschaftsPlaner Avalonia PoC

Dieses Projekt ist ein Proof-of-Concept für eine moderne Cross-Platform Version des BereitschaftsPlaner Tools.

## Framework & Technologie

- **Framework**: Avalonia UI 11.3.10 (Cross-Platform XAML)
- **Runtime**: .NET 9.0
- **Sprache**: C# 12 mit Nullable Reference Types
- **Architektur**: MVVM (Model-View-ViewModel)
- **MVVM Framework**: CommunityToolkit.Mvvm (Source Generators)
- **Datenbank**: LiteDB (NoSQL)
- **Excel**: ExcelDataReader (Cross-Platform, kein COM)
- **Logging**: Serilog (Strukturiertes Logging)

## WICHTIG: CODE QUALITY GUIDELINES

**VOR JEDER PROGRAMMIERUNG und NACH JEDEM BUILD ERROR:**

Lesen und befolgen: `~/.claude/CODE_QUALITY_GUIDELINES_AVALONIA.md`

Diese Guidelines enthalten:
- Namespace-Konflikte (SettingsService, MatchType)
- Property vs Type Name Kollisionen (Environment)
- XAML Binding Probleme (DataContext)
- Async/Await Patterns ([RelayCommand])
- Nullable Reference Types
- Service Dependencies
- Common Build Errors & Fixes
- Pre-Commit Checklist

**Bei Build-Fehlern:**
1. Fehler in CODE_QUALITY_GUIDELINES_AVALONIA.md nachschlagen
2. Fix anwenden
3. Wenn neuer Fehlertyp: Guidelines erweitern

## Projekt-Struktur

```
BereitschaftsPlaner.Avalonia/
├── Models/              # Data Models (Ressource, BereitschaftsGruppe, AppSettings)
├── ViewModels/          # ViewModels mit [ObservableProperty] und [RelayCommand]
├── Views/               # XAML Views (MainWindow, PlanningBoardView, etc.)
├── Services/            # Business Logic Services
│   ├── Data/            # DatabaseService, SettingsService (AppData)
│   ├── ExcelImportService.cs
│   ├── SBPUrlService.cs
│   └── ...
├── App.axaml.cs         # Application Entry, Static Services
└── Assets/              # Fonts, Icons
```

## MVVM Pattern

```
View (XAML)
  ↓ Binding
ViewModel (Logic, Commands, ObservableProperties)
  ↓ Uses
Model (Data Classes)
  ↓ Persisted by
Services (Database, Settings, Import/Export)
```

### Wichtige Services

#### Services.Data.SettingsService
- **Pfad**: `Services/Data/SettingsService.cs`
- **Methoden**: `GetSettings()`, `LoadSettings()`, `SaveSettings()`, `UpdateSetting<T>()`
- **Cached Access**: `GetSettings()` für Performance
- **Speicherort**: `%APPDATA%/BereitschaftsPlaner/settings.json`

#### Services.SettingsService (LEGACY - PowerShell Zeitprofile)
- **Pfad**: `Services/SettingsService.cs`
- **Methoden**: `LoadSettings()`, `SaveSettings()`
- **Verwendung**: NICHT verwenden in neuem Code!

#### Services.Data.DatabaseService
- **Pfad**: `Services/Data/DatabaseService.cs`
- **Datenbank**: LiteDB
- **Methoden**: `GetRessourcen()`, `SaveRessource()`, etc.

#### Services.SBPUrlService
- **Pfad**: `Services/SBPUrlService.cs`
- **Verwendung**: Environment-spezifische SBP URLs generieren
- **Environments**: QA, Production

## Entwicklungs-Workflow

### 1. Neue Funktion implementieren

```csharp
// IMMER erst CODE_QUALITY_GUIDELINES_AVALONIA.md lesen!

// ViewModel mit CommunityToolkit.Mvvm
public partial class MyViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [RelayCommand]
    private async Task DoSomething()  // async Task, NICHT async void!
    {
        await SomeAsyncOperation();
    }
}
```

### 2. XAML Binding

```xml
<!-- View hat eigenes DataContext (ViewModel) -->
<UserControl x:DataType="vm:MyViewModel">
    <!-- Command MUSS im MyViewModel existieren! -->
    <Button Command="{Binding DoSomethingCommand}"/>
</UserControl>
```

### 3. Settings verwenden

```csharp
using DataSettingsService = BereitschaftsPlaner.Avalonia.Services.Data.SettingsService;

private readonly DataSettingsService _settingsService;

public MyService(DataSettingsService settingsService)
{
    _settingsService = settingsService;
}

public void DoWork()
{
    var settings = _settingsService.GetSettings();
    // ...
}
```

## Build & Deployment

### GitHub Actions (Automatisch)
- Bei jedem Push wird Build ausgeführt
- Windows-Runner mit .NET SDK 9.0
- Artifacts werden erstellt

### Build Status prüfen
```bash
gh run list --limit 1
gh run view <run-id> --log-failed
gh run watch <run-id>
```

### KEIN lokaler Build auf diesem Server!
Grund: Linux-Server hat nur .NET 8.0, Projekt braucht .NET 9.0

## Theming

### Theme-Aware Classes verwenden
```xml
<!-- GUT - Theme-aware -->
<Button Classes="TrustAction" Content="Speichern"/>
<Border Background="{DynamicResource SurfaceBrush}"/>

<!-- VERMEIDEN - Hardcoded -->
<Button Background="#00CC7A" Foreground="White"/>
```

### Verfügbare Theme Classes
- `TrustAction` - Grüner Button (Save, OK, Export)
- `SecondaryAction` - Grauer Button (Cancel, Durchsuchen)
- `BentoCard` - Card mit Rounded Corners + Shadow
- `H1`, `H2`, `H3`, `Label`, `Body` - Text Styles

## Debugging

### Serilog Logging
```csharp
Serilog.Log.Debug($"Detail-Info: {value}");
Serilog.Log.Information($"Wichtiger Event: {value}");
Serilog.Log.Warning($"Potentielles Problem: {value}");
Serilog.Log.Error(ex, $"Fehler aufgetreten: {value}");
```

### Log-Dateien
- **Speicherort**: `%APPDATA%/BereitschaftsPlaner/Logs/`
- **Format**: `log-YYYYMMDD.txt`
- **Level**: Debug, Information, Warning, Error

## Testing

### Manuelle Tests
1. **Import View**: Excel Import mit Ressourcen/Gruppen testen
2. **Zeitprofile**: Profil erstellen/bearbeiten
3. **Planning**: 1-2 Tage generieren (NICHT ganze Woche!)
4. **Export**: Excel-Datei prüfen
5. **D365 Sandbox**: Import testen

## Häufige Fehlerquellen

### SettingsService Ambiguität
```csharp
// FALSCH - Ambiguous
using BereitschaftsPlaner.Avalonia.Services.Data;
private readonly SettingsService _service;

// RICHTIG - Using Alias
using DataSettingsService = BereitschaftsPlaner.Avalonia.Services.Data.SettingsService;
private readonly DataSettingsService _service;
```

### Environment Property Konflikt
```csharp
// FALSCH - Kollidiert mit System.Environment
public string Environment { get; set; }
Console.WriteLine(Environment);

// RICHTIG - this. verwenden
Console.WriteLine(this.Environment);

// BESSER - Anderen Namen verwenden
public int EnvironmentIndex { get; set; }
```

### Async void mit [RelayCommand]
```csharp
// FALSCH - Warning MVVMTK0039
[RelayCommand]
private async void DoWork() { ... }

// RICHTIG - async Task
[RelayCommand]
private async Task DoWork() { ... }
```

## Weiterführende Dokumentation

- `~/.claude/CODE_QUALITY_GUIDELINES_AVALONIA.md` - Comprehensive Development Guidelines
- [Avalonia UI Docs](https://docs.avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [ExcelDataReader](https://github.com/ExcelDataReader/ExcelDataReader)
- [LiteDB](https://www.litedb.org/)

---

**Version**: PoC 1.0
**Last Updated**: 2026-01-12
