using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class QuickStartGuideViewModel : ViewModelBase
{
    public ObservableCollection<HelpSection> HelpSections { get; } = new();

    public QuickStartGuideViewModel()
    {
        InitializeHelpSections();
    }

    private void InitializeHelpSections()
    {
        // Import-Tab
        HelpSections.Add(new HelpSection
        {
            Title = "Daten-Import",
            Icon = "",
            Content = """
                Excel-Daten importieren:

                1. Exportieren Sie aus Dynamics 365:
                   - Bereitschaftsgruppen (Bookable Resource Groups)
                   - Ressourcen (Bookable Resources)

                2. Klicken Sie auf "Excel importieren"

                3. Wählen Sie die Excel-Datei aus

                4. Die Daten werden automatisch konvertiert und in JSON gespeichert

                Nach dem Import können Sie die Daten in den anderen Tabs verwenden.
                """,
            Keywords = "Import, Excel, Daten, Bereitschaftsgruppen, Ressourcen, D365"
        });

        // Zeitprofile-Tab
        HelpSections.Add(new HelpSection
        {
            Title = "Zeitprofile",
            Icon = "",
            Content = """
                Zeitprofile verwalten:

                Bereitschaftstage definieren:
                - Startzeit: Wann beginnt die Bereitschaft (z.B. 16:00)
                - Endzeit: Wann endet die Bereitschaft (z.B. 07:30 am nächsten Tag)
                - Wochentage: Welche Tage sind Bereitschaftstage

                Tagesdienste definieren:
                - Ähnlich wie Bereitschaftstage, aber für reguläre Dienste (z.B. 07:30-16:00)

                Feiertage konfigurieren:
                - Bundesland auswählen
                - Region wählen (z.B. "Augsburg" für Bayern)
                - Behandlung festlegen: Ignorieren, Wie Werktag, Wie Wochenende

                Sie können mehrere Profile erstellen und Gruppen zuweisen.
                """,
            Keywords = "Zeitprofile, Bereitschaft, Tagesdienst, Feiertage, Bundesland, Wochentage"
        });

        // Planning-Board-Tab
        HelpSections.Add(new HelpSection
        {
            Title = "Planning Board",
            Icon = "",
            Content = """
                Bereitschaftsplan erstellen:

                1. Monat auswählen:
                   - Klicken Sie auf "Neuer Monat" (Strg+N)
                   - Oder navigieren Sie mit ← → (Strg+← / Strg+→)

                2. Zuweisungen vornehmen:
                   - Klicken Sie auf einen Tag
                   - Ziehen Sie Mitarbeiter per Drag & Drop
                   - Oder nutzen Sie die Auswahldialoge

                3. Auto-Fill nutzen:
                   - Klicken Sie auf "Auto-Fill" (Strg+M)
                   - Der Algorithmus plant den Monat automatisch
                   - Berücksichtigt: Urlaub, Feiertage, Fairness

                4. Exportieren:
                   - Excel: Für D365-Import (Strg+E)
                   - ICS: Für Kalender-Apps (Strg+I)

                Nutzen Sie Strg+F für das Fairness-Dashboard.
                """,
            Keywords = "Planning Board, Monat, Zuweisungen, Auto-Fill, Export, Excel, ICS, Kalender"
        });

        // Urlaubskalender
        HelpSections.Add(new HelpSection
        {
            Title = "Urlaubskalender",
            Icon = "",
            Content = """
                Urlaub verwalten:

                Urlaub hinzufügen:
                1. Klicken Sie auf "Neuer Urlaub"
                2. Mitarbeiter auswählen
                3. Startdatum und Enddatum eingeben
                4. Optional: Notiz hinzufügen
                5. Speichern

                Urlaub bearbeiten/löschen:
                - Doppelklick auf Eintrag → Bearbeiten
                - Rechtsklick → Löschen

                Import/Export:
                - Urlaubsliste als Excel exportieren
                - Externe Daten importieren

                Der Auto-Fill-Algorithmus berücksichtigt automatisch alle Urlaube.
                """,
            Keywords = "Urlaub, Kalender, Ferien, Abwesenheit, Import, Export"
        });

        // Templates
        HelpSections.Add(new HelpSection
        {
            Title = "Templates",
            Icon = "",
            Content = """
                Template-Bibliothek:

                Template speichern:
                1. Erstellen Sie einen Bereitschaftsplan
                2. Klicken Sie auf "Template speichern" (Strg+S)
                3. Geben Sie einen Namen ein (z.B. "Standard August")
                4. Optional: Beschreibung hinzufügen

                Template laden:
                1. Klicken Sie auf "Template laden" (Strg+O)
                2. Wählen Sie ein Template aus der Liste
                3. Bestätigen Sie das Laden

                Verwendung:
                - Wiederkehrende Muster speichern
                - Jahreswechsel vorbereiten
                - Schnellstart für neue Monate

                Templates enthalten alle Zuweisungen, aber KEINE Urlaubsdaten.
                """,
            Keywords = "Templates, Speichern, Laden, Muster, Bibliothek"
        });

        // Fairness & Analytics
        HelpSections.Add(new HelpSection
        {
            Title = "Fairness & Analytics",
            Icon = "",
            Content = """
                Fairness-Dashboard:

                Metriken verstehen:
                - Bereitschaftstage: Anzahl zugewiesener Bereitschaften
                - Wochenend-Dienste: Anzahl Samstag/Sonntag-Einsätze
                - Feiertags-Dienste: Anzahl Feiertags-Einsätze
                - Fairness-Score: 0-100% (100% = perfekt fair)

                Historische Analyse:
                - Trends über mehrere Monate
                - Vergleich zwischen Mitarbeitern
                - Identifikation von Ungleichgewichten

                What-If-Szenarien:
                - Simulieren Sie Änderungen
                - Vergleichen Sie Alternativen
                - Optimieren Sie die Verteilung

                Streben Sie einen Fairness-Score > 85% an.
                """,
            Keywords = "Fairness, Analytics, Dashboard, Report, Score, Metriken, Analyse"
        });

        // Tastenkombinationen
        HelpSections.Add(new HelpSection
        {
            Title = "Tastenkombinationen",
            Icon = "",
            Content = """
                Wichtigste Shortcuts:

                Navigation:
                - Strg + 1-5: Tabs wechseln
                - Strg + Tab: Nächster Tab
                - F1: Diese Hilfe
                - Strg + ?: Alle Tastenkombinationen

                Planning Board:
                - Strg + N: Neuer Monat
                - Strg + ←/→: Monat wechseln
                - Strg + M: Auto-Fill
                - Strg + D: Alle löschen

                Export:
                - Strg + E: Excel exportieren
                - Strg + I: ICS exportieren
                - Strg + S: Template speichern

                Fenster:
                - Strg + K: Urlaubskalender
                - Strg + F: Fairness-Dashboard
                - Esc: Dialog schließen

                Vollständige Liste: Strg + ?
                """,
            Keywords = "Tastenkombinationen, Shortcuts, Keyboard, Strg, F1, Navigation"
        });

        // Troubleshooting
        HelpSections.Add(new HelpSection
        {
            Title = "Problembehebung",
            Icon = "",
            Content = """
                Häufige Probleme:

                "Excel-Import schlägt fehl"
                → Prüfen Sie, ob die Excel-Datei von D365 exportiert wurde
                → Stellen Sie sicher, dass die Datei nicht geöffnet ist
                → Prüfen Sie die Spaltenstruktur

                "Auto-Fill funktioniert nicht"
                → Mindestens 2 Mitarbeiter müssen verfügbar sein
                → Prüfen Sie Urlaubseinträge
                → Prüfen Sie Zeitprofile-Konfiguration

                "Konflikte bei Zuweisungen"
                → Mitarbeiter können nicht an mehreren Gruppen gleichzeitig sein
                → Prüfen Sie überlappende Zeiträume
                → Nutzen Sie "Konflikte prüfen"

                "Export nach D365 schlägt fehl"
                → Prüfen Sie das Template (config/template.xlsx)
                → Stellen Sie sicher, dass alle Pflichtfelder gefüllt sind
                → Prüfen Sie D365-Berechtigungen

                Bei weiteren Fragen wenden Sie sich an den Support.
                """,
            Keywords = "Problem, Fehler, Troubleshooting, Import fehlgeschlagen, Export fehlgeschlagen"
        });
    }
}

public class HelpSection
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
}
