using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class WelcomeWizardViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _currentStep = 0;

    [ObservableProperty]
    private string _currentTitle = "Willkommen!";

    [ObservableProperty]
    private string _currentContent = "";

    public ObservableCollection<WizardStep> Steps { get; } = new();

    public bool IsFirstStep => CurrentStep == 0;
    public bool IsLastStep => CurrentStep == Steps.Count - 1;
    public string NextButtonText => IsLastStep ? "Fertig" : "Weiter";

    public WelcomeWizardViewModel()
    {
        InitializeSteps();
        UpdateCurrentStep();
    }

    private void InitializeSteps()
    {
        Steps.Add(new WizardStep
        {
            Title = "Willkommen zum Bereitschafts-Planer",
            Content = "Diese Anwendung hilft Ihnen, Bereitschaftsdienste für Microsoft Dynamics 365 Field Service effizient zu planen.\n\n" +
                     "In wenigen Schritten zeigen wir Ihnen die wichtigsten Funktionen.\n\n" +
                     "Klicken Sie auf 'Weiter' um zu starten.",
            Icon = ""
        });

        Steps.Add(new WizardStep
        {
            Title = "1. Daten importieren",
            Content = "Schritt 1: Excel-Import\n\n" +
                     "Exportieren Sie aus Dynamics 365:\n" +
                     "• Ressourcen (Mitarbeiter)\n" +
                     "• Bereitschaftsgruppen\n\n" +
                     "Importieren Sie diese im Tab 'Import'.\n\n" +
                     "Die Daten werden automatisch in JSON konvertiert und gespeichert.",
            Icon = ""
        });

        Steps.Add(new WizardStep
        {
            Title = "2. Zeitprofile konfigurieren",
            Content = "Schritt 2: Arbeitszeiten definieren\n\n" +
                     "Im Tab 'Zeitprofile' legen Sie fest:\n" +
                     "• Bereitschaftszeiten (BD): z.B. 16:00-07:30\n" +
                     "• Tagesdienste (TD): z.B. 07:30-16:00\n" +
                     "• Feiertags-Regelungen pro Bundesland\n\n" +
                     "Sie können mehrere Profile erstellen (z.B. 'Standard', 'Augsburg').",
            Icon = ""
        });

        Steps.Add(new WizardStep
        {
            Title = "3. Planning Board nutzen",
            Content = "Schritt 3: Dienste planen\n\n" +
                     "Das Planning Board ist Ihr Hauptwerkzeug:\n\n" +
                     "Manuelle Planung:\n" +
                     "• Klicken Sie auf einen Tag oder 'KW X'\n" +
                     "• Wählen Sie Ressource und Gruppe\n\n" +
                     "Auto-Fill:\n" +
                     "• 1-Klick Monatsplanung\n" +
                     "• Faire automatische Verteilung\n" +
                     "• Berücksichtigt Urlaub & Fairness",
            Icon = ""
        });

        Steps.Add(new WizardStep
        {
            Title = "4. Urlaub verwalten",
            Content = "Schritt 4: Urlaubskalender\n\n" +
                     "Klicken Sie auf 'Urlaubskalender':\n\n" +
                     "• Urlaub, Krankheit, Fortbildung eintragen\n" +
                     "• Datum-Bereiche möglich (Von-Bis)\n" +
                     "• Auto-Fill berücksichtigt Urlaube automatisch\n\n" +
                     "So vermeiden Sie Konflikte!",
            Icon = ""
        });

        Steps.Add(new WizardStep
        {
            Title = "5. Templates & Export",
            Content = "Schritt 5: Speichern & Exportieren\n\n" +
                     "Templates:\n" +
                     "• Erfolgreiche Planungen als Vorlage speichern\n" +
                     "• Für andere Monate wiederverwenden\n\n" +
                     "Excel-Export:\n" +
                     "• Basiert auf D365-Template (config/template.xlsx)\n" +
                     "• Direkt in D365 importierbar\n\n" +
                     "ICS-Export:\n" +
                     "• Für Outlook/Google Calendar",
            Icon = ""
        });

        Steps.Add(new WizardStep
        {
            Title = "Bereit zum Start",
            Content = "Sie sind startklar!\n\n" +
                     "Weitere Features:\n" +
                     "• Fairness-Dashboard\n" +
                     "• Konflikt-Erkennung\n" +
                     "• Historische Analyse\n" +
                     "• Feature-Flags (Features-Button)\n\n" +
                     "Tastenkombinationen:\n" +
                     "• Strg+? → Alle Shortcuts anzeigen\n" +
                     "• F1 → Schnellstart-Hilfe\n\n" +
                     "Viel Erfolg mit dem Bereitschafts-Planer!",
            Icon = ""
        });
    }

    private void UpdateCurrentStep()
    {
        if (CurrentStep >= 0 && CurrentStep < Steps.Count)
        {
            var step = Steps[CurrentStep];
            CurrentTitle = step.Title;
            CurrentContent = step.Content;
        }

        OnPropertyChanged(nameof(IsFirstStep));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(NextButtonText));
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < Steps.Count - 1)
        {
            CurrentStep++;
            UpdateCurrentStep();
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
            UpdateCurrentStep();
        }
    }

    [RelayCommand]
    private void SkipWizard()
    {
        // Will be handled by window close
    }
}

public class WizardStep
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
