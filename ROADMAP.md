# Bereitschaftsplaner Avalonia - Development Roadmap

**Ziel:** Feature-Parität mit PowerShell v3.8.5 + moderne .NET Best Practices (12/2025)

**Status:** PoC abgeschlossen (v1.0.0-poc) → Produktions-Version in Entwicklung

---

## 📊 Feature-Parität mit PowerShell v3.8.5

### Aktueller Stand PowerShell:
- ✅ Tab 1: Setup - Excel Import (Ressourcen + Bereitschaftsgruppen)
- ✅ Tab 2: Zeitprofile - Konfiguration (BD/TD mit Wochenplan)
- ✅ Tab 3: Generator - Bereitschaftslisten erstellen
- ✅ Tab 4: Editor - Listen bearbeiten (Filter, Ressource ändern)
- ✅ Tab 5: Statistiken - Auswertungen + CSV Export

### Avalonia PoC Stand (v1.0.0-poc):
- ✅ Tab 1: Ressourcen Import (Excel → JSON)
- ✅ Tab 2: Technologie-Vergleich (Info)
- ❌ Bereitschaftsgruppen Import
- ❌ Zeitprofile
- ❌ Generator
- ❌ Editor
- ❌ Statistiken

---

## 🏗️ Architektur & Best Practices (12/2025)

### Data Storage Strategy

**Settings (User Preferences):**
```
✅ Verwenden: JSON File Storage (keine zusätzliche NuGet-Dependency)
- Plattform-übergreifend (gleiche AppData-Ordner wie Datenbank)
- JSON-Datei: settings.json
- Einfach, zuverlässig, keine externen Dependencies

Beispiel:
public class AppSettings
{
    public string LastImportPath { get; set; }
    public string Bundesland { get; set; } = "BY";
    public string FeiertagsBehandlung { get; set; } = "Sonntag";
}

// Laden/Speichern
var settings = settingsService.LoadSettings();
settingsService.SaveSettings(settings);

// Speicherort: %APPDATA%/BereitschaftsPlaner/settings.json
```

**Daten (Ressourcen, Gruppen, Bereitschaften):**
```
✅ Verwenden: LiteDB (NuGet: LiteDB 5.0.x)
- NoSQL Dokument-Datenbank (wie MongoDB)
- Single-File (bereitschaftsplaner.db)
- Automatische Backups vor Schema-Änderungen
- JSON-kompatibel für Migration

Warum LiteDB statt SQLite?
- Einfacher: Keine Schema-Definitionen nötig
- JSON-like: Einfache Migration von PowerShell JSON
- Embedded: Keine externe DB nötig
- Backups: Einfach File kopieren

Beispiel:
using var db = new LiteDatabase("bereitschaftsplaner.db");
var ressourcen = db.GetCollection<Ressource>("ressourcen");
ressourcen.Insert(new Ressource { Name = "...", Bezirk = "..." });
var all = ressourcen.FindAll();
```

**Template & Outputs:**
```
✅ Dateisystem (wie bisher)
- config/template.xlsx
- output/*.xlsx
- logs/*.log

Pfade:
- Windows: %APPDATA%/BereitschaftsPlaner/
- macOS: ~/Library/Application Support/BereitschaftsPlaner/
- Linux: ~/.config/BereitschaftsPlaner/
```

### Migration Strategy (PowerShell JSON → LiteDB)

```csharp
// Bei erstem Start: JSON importieren falls vorhanden
public class MigrationService
{
    public async Task MigrateFromPowerShellJson()
    {
        var jsonPath = Path.Combine(AppDataPath, "config", "ressourcen.json");
        if (File.Exists(jsonPath))
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            var ressourcen = JsonSerializer.Deserialize<List<Ressource>>(json);

            using var db = new LiteDatabase(DbPath);
            var collection = db.GetCollection<Ressource>("ressourcen");
            collection.InsertBulk(ressourcen);

            // Backup der JSON erstellen
            File.Move(jsonPath, jsonPath + ".migrated.bak");
        }
    }
}
```

### Update-Safety (Keine Datenverluste)

```csharp
// Vor App-Start: Backup der DB
public class BackupService
{
    public void CreateBackupBeforeUpdate()
    {
        var dbPath = Path.Combine(AppDataPath, "bereitschaftsplaner.db");
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var backupPath = Path.Combine(AppDataPath, "backups",
            $"bereitschaftsplaner_v{version}_{DateTime.Now:yyyyMMdd_HHmmss}.db");

        if (File.Exists(dbPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
            File.Copy(dbPath, backupPath);

            // Alte Backups aufräumen (nur letzte 10 behalten)
            CleanupOldBackups(maxBackups: 10);
        }
    }
}

// In App.axaml.cs OnFrameworkInitializationCompleted:
new BackupService().CreateBackupBeforeUpdate();
```

---

## 📅 Phasen-Plan

### Phase 1: Projekt-Setup & Infrastruktur ✅ DONE
- [x] .NET 9.0 Projekt erstellt
- [x] Avalonia 11.3.10 konfiguriert
- [x] MVVM mit CommunityToolkit.Mvvm
- [x] Dark Mode Support
- [x] Basic UI (2 Tabs)

### Phase 2: Data Layer & Services ✅ DONE
- [x] LiteDB Integration (NuGet: LiteDB)
- [x] Settings Integration (JSON File Storage - keine extra NuGet)
- [x] Models definieren (Ressource, BereitschaftsGruppe, Zeitprofil, etc.)
- [x] DatabaseService (CRUD Operations)
- [x] SettingsService (JSON-based User Preferences)
- [x] BackupService (Automatische Backups)
- [x] MigrationService (PowerShell JSON → LiteDB)
- [x] App startup integration (automatic backups, migration, initialization)
- [x] MainWindowViewModel updated to use DatabaseService

**Completed:** Phase 2 abgeschlossen - Vollständige Datenbank-Integration

### Phase 3: Tab 1 - Setup & Import 🔄 PARTIAL
- [x] Ressourcen Excel-Import (ExcelDataReader)
- [x] Ressourcen → LiteDB speichern
- [ ] Bereitschaftsgruppen Excel-Import
- [ ] Bereitschaftsgruppen → LiteDB speichern
- [ ] Import-Preview Dialog
- [ ] Spalten-Mapping Dialog
- [ ] Validierung & Fehlerbehandlung
- [ ] UI-Feedback (Progress, Status)

**Estimated Time:** 10-15 Stunden

### Phase 4: Tab 2 - Zeitprofile
- [ ] ZeitprofilManager Service
- [ ] Zeitprofil Model (BD/TD + Wochentage)
- [ ] UI: Profil-Auswahl Dropdown
- [ ] UI: Wochentage-Grid (Mo-So)
- [ ] UI: Zeit-Picker (StartZeit, EndZeit, Folgetag)
- [ ] Speichern/Laden von Preferences
- [ ] Standard-Zeitprofile (BD: 16:00-07:30, TD: 07:30-16:00)

**Estimated Time:** 8-12 Stunden

### Phase 5: Tab 3 - Generator
- [ ] BereitschaftsGenerator Service
- [ ] Feiertags-Manager (PublicHoliday NuGet)
- [ ] Template.xlsx Handling (ClosedXML NuGet)
- [ ] UI: Monat/Jahr Auswahl
- [ ] UI: Feiertags-Einstellungen (Bundesland, Region)
- [ ] UI: Gruppe & Zeitprofil Auswahl
- [ ] Excel-Generierung
- [ ] Output-Verwaltung (Datei öffnen, Speicherort)

**Estimated Time:** 15-20 Stunden

### Phase 6: Tab 4 - Editor
- [ ] BereitschaftsEditor Service
- [ ] Excel-Import (bestehende Listen)
- [ ] UI: DataGrid mit Bereitschaften
- [ ] UI: Filter (Datum, Gruppe, Ressource)
- [ ] UI: Ressource ändern (Multi-Select)
- [ ] UI: Speichern-Funktion
- [ ] Undo/Redo Support (optional)

**Estimated Time:** 12-18 Stunden

### Phase 7: Tab 5 - Statistiken
- [ ] StatistikManager Service
- [ ] Berechnungen (Pro Ressource, Pro Gruppe, Gesamt)
- [ ] UI: Statistik-Übersicht (ListViews)
- [ ] UI: CSV Export
- [ ] UI: Diagramme (LiveCharts oder OxyPlot - optional)

**Estimated Time:** 8-12 Stunden

### Phase 8: Polishing & Testing
- [ ] Error-Handling durchgängig
- [ ] Loading-Indikatoren
- [ ] Tooltips & Hilfe-Texte
- [ ] Keyboard-Shortcuts
- [ ] Integration-Tests
- [ ] Performance-Optimierung
- [ ] Accessibility (optional)

**Estimated Time:** 10-15 Stunden

### Phase 9: Deployment & Distribution
- [ ] Publish-Profile (Windows, macOS, Linux)
- [ ] Self-Contained Builds
- [ ] Setup-Installer (optional: MSIX für Windows)
- [ ] GitHub Release
- [ ] User-Dokumentation

**Estimated Time:** 5-8 Stunden

---

## 📦 NuGet Packages (Best Practices 12/2025)

### Core:
```xml
<PackageReference Include="Avalonia" Version="11.3.10" />
<PackageReference Include="Avalonia.Desktop" Version="11.3.10" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.10" />
<PackageReference Include="Avalonia.Controls.DataGrid" Version="11.3.10" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
```

### Data & Storage:
```xml
<PackageReference Include="LiteDB" Version="5.0.21" />
<!-- Settings: JSON File Storage (keine NuGet-Dependency nötig) -->
```

### Excel Handling:
```xml
<PackageReference Include="ExcelDataReader" Version="3.7.0" />
<PackageReference Include="ExcelDataReader.DataSet" Version="3.7.0" />
<PackageReference Include="ClosedXML" Version="0.104.1" />  <!-- Für Template-Generierung -->
<PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.0" />
```

### Utilities:
```xml
<PackageReference Include="PublicHoliday" Version="2.45.0" />  <!-- Feiertage -->
<PackageReference Include="Serilog" Version="4.2.0" />  <!-- Logging -->
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
```

### Optional (Charts):
```xml
<PackageReference Include="LiveChartsCore.SkiaSharpView.Avalonia" Version="2.0.0-rc4" />
```

---

## 🗂️ Projekt-Struktur (Clean Architecture)

```
BereitschaftsPlaner.Avalonia/
├── Models/
│   ├── Ressource.cs
│   ├── BereitschaftsGruppe.cs
│   ├── Zeitprofil.cs
│   ├── Bereitschaft.cs
│   └── AppSettings.cs
├── Services/
│   ├── Data/
│   │   ├── DatabaseService.cs
│   │   ├── SettingsService.cs
│   │   └── BackupService.cs
│   ├── Import/
│   │   ├── ExcelImportService.cs
│   │   └── MigrationService.cs
│   ├── Business/
│   │   ├── ZeitprofilManager.cs
│   │   ├── BereitschaftsGenerator.cs
│   │   ├── BereitschaftsEditor.cs
│   │   ├── StatistikManager.cs
│   │   └── FeiertagsManager.cs
│   └── Export/
│       └── ExcelExportService.cs
├── ViewModels/
│   ├── ViewModelBase.cs
│   ├── MainWindowViewModel.cs
│   ├── Tab1SetupViewModel.cs
│   ├── Tab2ZeitprofileViewModel.cs
│   ├── Tab3GeneratorViewModel.cs
│   ├── Tab4EditorViewModel.cs
│   └── Tab5StatistikenViewModel.cs
├── Views/
│   ├── MainWindow.axaml
│   ├── Tab1SetupView.axaml
│   ├── Tab2ZeitprofileView.axaml
│   ├── Tab3GeneratorView.axaml
│   ├── Tab4EditorView.axaml
│   └── Tab5StatistikenView.axaml
├── Converters/
│   └── BoolToVisibilityConverter.cs
├── Assets/
│   └── Icons/
├── App.axaml
└── Program.cs
```

---

## 🎯 Gesamt-Aufwand (Schätzung)

| Phase | Stunden | Status |
|-------|---------|--------|
| Phase 1: Setup | 8 | ✅ DONE |
| Phase 2: Data Layer | 8-12 | ✅ DONE |
| Phase 3: Tab 1 | 10-15 | 🔄 NEXT |
| Phase 4: Tab 2 | 8-12 | 📋 PLANNED |
| Phase 5: Tab 3 | 15-20 | 📋 PLANNED |
| Phase 6: Tab 4 | 12-18 | 📋 PLANNED |
| Phase 7: Tab 5 | 8-12 | 📋 PLANNED |
| Phase 8: Polishing | 10-15 | 📋 PLANNED |
| Phase 9: Deployment | 5-8 | 📋 PLANNED |
| **GESAMT** | **84-120 Std** | |

**Realistisch:** ~100 Stunden für Feature-Parität + moderne Best Practices

---

## 📝 Nächste Schritte (Phase 2)

### 1. LiteDB Integration
```bash
dotnet add package LiteDB --version 5.0.21
```

### 2. Models erweitern
```csharp
public class Ressource
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Bezirk { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BereitschaftsGruppe
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Bezirk { get; set; }
    public string VerantwortlichePerson { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. DatabaseService erstellen
```csharp
public class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "BereitschaftsPlaner");
        Directory.CreateDirectory(appFolder);
        _dbPath = Path.Combine(appFolder, "bereitschaftsplaner.db");
    }

    public List<Ressource> GetAllRessourcen()
    {
        using var db = new LiteDatabase(_dbPath);
        return db.GetCollection<Ressource>("ressourcen").FindAll().ToList();
    }

    public void SaveRessourcen(List<Ressource> ressourcen)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Ressource>("ressourcen");
        collection.DeleteAll();
        collection.InsertBulk(ressourcen);
    }
}
```

---

## ⚠️ Breaking Changes & Migration

### Von PoC (v1.0.0) zu Produktions-Version:

**Datenbank-Migration:**
- JSON-Export aus PoC erstellen
- Beim ersten Start: JSON → LiteDB migrieren
- JSON als Backup behalten (.migrated.bak)

**Settings-Migration:**
- Alte Preferences auslesen
- In neue Struktur überführen
- Version-Tag in Settings speichern

**Template-Migration:**
- Template.xlsx in neue Ordnerstruktur kopieren
- Pfad in Settings aktualisieren

---

## 📅 Changelog

### v1.0.0-poc (2025-12-19)
- ✅ Initial PoC mit Excel-Import
- ✅ .NET 9.0 + Avalonia 11.3.10
- ✅ MVVM Pattern
- ✅ Dark Mode Support
- ✅ Cross-Platform Build

### v2.0.0 (geplant)
- 🔄 LiteDB Integration
- 🔄 Alle 5 Tabs implementiert
- 🔄 Feature-Parität mit PowerShell v3.8.5
- 🔄 Automatische Backups
- 🔄 Migration von PowerShell JSON

---

**Letzte Aktualisierung:** 2025-12-19
**Status:** Phase 2 abgeschlossen ✅
**Nächster Milestone:** Phase 3 - Bereitschaftsgruppen Import + Validierung
