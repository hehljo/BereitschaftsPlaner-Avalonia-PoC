using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class KeyboardShortcutsViewModel : ViewModelBase
{
    public ObservableCollection<ShortcutGroup> ShortcutGroups { get; } = new();

    public KeyboardShortcutsViewModel()
    {
        InitializeShortcuts();
    }

    private void InitializeShortcuts()
    {
        // Navigation
        ShortcutGroups.Add(new ShortcutGroup
        {
            Name = "Navigation",
            Shortcuts = new ObservableCollection<KeyboardShortcut>
            {
                new() { Keys = "Strg + 1-5", Description = "Zwischen Tabs wechseln (Import, Zeitprofile, Generator, Planning, Editor)" },
                new() { Keys = "Strg + Tab", Description = "Nächster Tab" },
                new() { Keys = "Strg + Shift + Tab", Description = "Vorheriger Tab" },
                new() { Keys = "F1", Description = "Schnellstart-Hilfe öffnen" },
                new() { Keys = "Strg + ?", Description = "Diese Tastenkombinationen anzeigen" },
            }
        });

        // Planning Board
        ShortcutGroups.Add(new ShortcutGroup
        {
            Name = "Planning Board",
            Shortcuts = new ObservableCollection<KeyboardShortcut>
            {
                new() { Keys = "Strg + N", Description = "Neuer Monat (Navigation)" },
                new() { Keys = "Strg + ←", Description = "Vorheriger Monat" },
                new() { Keys = "Strg + →", Description = "Nächster Monat" },
                new() { Keys = "Strg + T", Description = "Heute (aktueller Monat)" },
                new() { Keys = "Strg + M", Description = "Auto-Fill (Monat automatisch planen)" },
                new() { Keys = "Strg + D", Description = "Alle Zuordnungen löschen" },
            }
        });

        // Templates & Export
        ShortcutGroups.Add(new ShortcutGroup
        {
            Name = "Templates & Export",
            Shortcuts = new ObservableCollection<KeyboardShortcut>
            {
                new() { Keys = "Strg + S", Description = "Template speichern" },
                new() { Keys = "Strg + O", Description = "Template laden" },
                new() { Keys = "Strg + E", Description = "Nach Excel exportieren" },
                new() { Keys = "Strg + I", Description = "ICS exportieren (Kalender)" },
                new() { Keys = "Strg + R", Description = "Fairness-Report anzeigen" },
            }
        });

        // Dialoge & Fenster
        ShortcutGroups.Add(new ShortcutGroup
        {
            Name = "Fenster & Dialoge",
            Shortcuts = new ObservableCollection<KeyboardShortcut>
            {
                new() { Keys = "Strg + ,", Description = "Einstellungen öffnen" },
                new() { Keys = "Strg + K", Description = "Urlaubskalender öffnen" },
                new() { Keys = "Strg + H", Description = "Historische Analyse öffnen" },
                new() { Keys = "Strg + F", Description = "Fairness-Dashboard öffnen" },
                new() { Keys = "Esc", Description = "Dialog schließen" },
                new() { Keys = "Alt + F4", Description = "Anwendung beenden" },
            }
        });

        // Allgemein
        ShortcutGroups.Add(new ShortcutGroup
        {
            Name = "Allgemein",
            Shortcuts = new ObservableCollection<KeyboardShortcut>
            {
                new() { Keys = "Strg + Z", Description = "Rückgängig (falls verfügbar)" },
                new() { Keys = "Strg + Y", Description = "Wiederherstellen (falls verfügbar)" },
                new() { Keys = "Strg + C", Description = "Kopieren" },
                new() { Keys = "Strg + V", Description = "Einfügen" },
                new() { Keys = "Strg + A", Description = "Alles auswählen" },
                new() { Keys = "F5", Description = "Daten neu laden" },
            }
        });
    }
}

public class ShortcutGroup
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<KeyboardShortcut> Shortcuts { get; set; } = new();
}

public class KeyboardShortcut
{
    public string Keys { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
