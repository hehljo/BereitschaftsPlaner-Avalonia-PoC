# Changelog

All notable changes to BereitschaftsPlaner Avalonia will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-beta] - 2025-12-21

### 🎉 Initial Beta Release

Complete rewrite of PowerShell v3.8.2 in Avalonia/.NET with 100% feature parity.

### ✨ Added

#### Core Features
- **Modern UI** - Cross-platform Avalonia interface with Yunex branding
- **Import System** - Excel to database import with preview & validation
- **Excel Generation** - Template-based on-call schedule creation
- **Excel Editor** - Inline editing of existing schedules
- **Time Profiles** - Multi-profile system with group assignments
- **Holiday Management** - 16 German states + regions (API + fallback)
- **Database** - LiteDB with automatic backups

#### User Interface
- **Tab 1: Import** - Excel import with validation & preview
- **Tab 2: Zeitprofile** - Time profile configuration
- **Tab 3: Generator** - On-call schedule generation
- **Tab 4: Editor** - Excel file editing
- **Dark Mode** - Theme switcher with persistence
- **Environment Switcher** - Production/QA toggle
- **Status Bar** - Real-time operation feedback

#### Import System (v3.8.x Parity)
- **Import Preview Dialog** - See data before saving to database
- **Data Validation** - Automatic duplicate detection & cleaning
- **Error Reporting** - Detailed validation with warnings
- **Column Auto-Detection** - Flexible Excel column mapping
- **Confirmation Required** - User must approve before save

#### Holiday System
- **API Integration** - feiertage-api.de with 10s timeout
- **Local Fallback** - Gauss algorithm for Easter calculation
- **Smart Caching** - Memory → File → API → Local
- **19 Pre-loaded JSONs** - All states for 2025/2026
- **Regional Support** - Bavaria: Augsburg, Catholic regions

#### Database & Backup
- **LiteDB Storage** - Embedded NoSQL database
- **Automatic Backups** - Before updates
- **Manual Backups** - Reset database with backup creation
- **Cross-Platform Paths** - AppData storage

#### Excel Integration
- **ClosedXML** - Real .xlsx manipulation (cross-platform)
- **Template System** - D365 metadata preservation
- **ExcelDataReader** - Cross-platform Excel import
- **Overnight Shifts** - Correct date calculation (16:00 → next day 07:30)

### 🚀 Performance

- **10-100x Faster** than PowerShell version
- **~80 MB Memory** vs. ~150 MB PowerShell
- **~1s Startup** vs. ~3-5s PowerShell
- **Async/Await** - No UI freezes

### 🔧 Technical

- **.NET 9.0** - Latest LTS version
- **Avalonia 11.3.10** - Cross-platform XAML
- **CommunityToolkit.Mvvm 8.2.1** - MVVM framework
- **LiteDB 5.0.21** - Embedded database
- **ClosedXML 0.104.2** - Excel manipulation
- **ExcelDataReader 3.7.0** - Excel import

### 📦 Build & Deploy

- **GitHub Actions** - Automated builds
- **Cross-Platform** - Windows, macOS, Linux binaries
- **Installers** - Windows EXE, macOS DMG, Linux AppImage

### 🐛 Fixed (vs. PowerShell v3.8.2)

- ✅ No "op_Subtraction" error with wrong Excel files
- ✅ No multiple warning dialogs after import
- ✅ No false "success" message when import fails
- ✅ No UI freezes during long operations
- ✅ No Excel COM dependency (cross-platform)

### ⚡ Changed from PowerShell

- **Database** - JSON files → LiteDB
- **Excel Library** - COM → ClosedXML
- **UI Framework** - WinForms → Avalonia
- **Language** - PowerShell → C# (.NET 9)
- **Performance** - Script → Compiled

### 🌍 Platform Support

- ✅ **Windows** - Windows 10/11 (x64/ARM64)
- ✅ **macOS** - macOS 11+ (x64/ARM64)
- ✅ **Linux** - Ubuntu 20.04+ (x64/ARM64)

### 📚 Documentation

- Professional README.md with badges
- CODE_QUALITY_GUIDELINES.md
- COMPARISON.md (PowerShell vs. Avalonia)
- This CHANGELOG.md

---

## [Unreleased]

### Planned for v1.1.0
- Column mapping UI dialog
- Advanced Excel editor
- Export to JSON (PowerShell compatibility)
- Import history & rollback
- Multi-language support (EN/DE)

### Planned for v1.2.0
- Direct D365 API integration
- Calendar view
- Conflict detection
- Notification system
- Cloud sync

---

## Migration from PowerShell

### Breaking Changes
- **Database Format** - JSON → LiteDB (automatic migration on first start)
- **Configuration** - PowerShell modules → .NET services
- **Platform** - Windows-only → Cross-platform

### Compatibility
- ✅ **Excel Templates** - 100% compatible
- ✅ **D365 Import** - Same format
- ✅ **Holiday JSONs** - Same structure
- ✅ **Time Profiles** - Same logic

### Migration Path
1. Export data from PowerShell (JSON)
2. Start Avalonia app (auto-migrates on first run)
3. Verify data in Tab 1
4. Test generation in Tab 3

---

[1.0.0-beta]: https://github.com/hehljo/BereitschaftsPlaner-Avalonia-PoC/releases/tag/v1.0.0-beta
[Unreleased]: https://github.com/hehljo/BereitschaftsPlaner-Avalonia-PoC/compare/v1.0.0-beta...HEAD
