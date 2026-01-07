using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using BereitschaftsPlaner.Avalonia.Models;
using BereitschaftsPlaner.Avalonia.Services.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace BereitschaftsPlaner.Avalonia.ViewModels;

public partial class TemplateLibraryViewModel : ViewModelBase
{
    private readonly TemplateLibraryService _templateService;
    private readonly Action<PlanningTemplate>? _onTemplateSelected;

    [ObservableProperty]
    private ObservableCollection<PlanningTemplate> _templates = new();

    [ObservableProperty]
    private ObservableCollection<string> _categories = new();

    [ObservableProperty]
    private PlanningTemplate? _selectedTemplate;

    [ObservableProperty]
    private string _selectedCategory = "Alle";

    public TemplateLibraryViewModel()
    {
        _templateService = App.TemplateLibraryService;
        LoadTemplates();
        LoadCategories();
    }

    public TemplateLibraryViewModel(Action<PlanningTemplate> onTemplateSelected) : this()
    {
        _onTemplateSelected = onTemplateSelected;
    }

    /// <summary>
    /// Load all templates
    /// </summary>
    private void LoadTemplates()
    {
        try
        {
            var allTemplates = _templateService.GetAllTemplates();

            if (SelectedCategory == "Alle")
            {
                Templates = new ObservableCollection<PlanningTemplate>(allTemplates);
            }
            else
            {
                var filtered = allTemplates.Where(x => x.Category == SelectedCategory);
                Templates = new ObservableCollection<PlanningTemplate>(filtered);
            }

            Log.Information("Templates geladen: {Count}", Templates.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden der Templates");
        }
    }

    /// <summary>
    /// Load categories
    /// </summary>
    private void LoadCategories()
    {
        try
        {
            var cats = _templateService.GetCategories();
            cats.Insert(0, "Alle");
            Categories = new ObservableCollection<string>(cats);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Laden der Kategorien");
        }
    }

    /// <summary>
    /// Category selection changed
    /// </summary>
    partial void OnSelectedCategoryChanged(string value)
    {
        LoadTemplates();
    }

    /// <summary>
    /// Delete selected template
    /// </summary>
    [RelayCommand]
    private async void DeleteTemplate()
    {
        if (SelectedTemplate == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Kein Template ausgewählt",
                "Bitte wählen Sie ein Template zum Löschen aus.",
                ButtonEnum.Ok,
                Icon.Warning
            ).ShowAsync();
            return;
        }

        var result = await MessageBoxManager.GetMessageBoxStandard(
            "Template löschen?",
            $"Möchten Sie das Template '{SelectedTemplate.Name}' wirklich löschen?",
            ButtonEnum.YesNo,
            Icon.Question
        ).ShowAsync();

        if (result == ButtonResult.Yes)
        {
            try
            {
                _templateService.DeleteTemplate(SelectedTemplate.Id);
                LoadTemplates();
                LoadCategories();

                await MessageBoxManager.GetMessageBoxStandard(
                    "Gelöscht",
                    "Template erfolgreich gelöscht.",
                    ButtonEnum.Ok,
                    Icon.Success
                ).ShowAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Löschen des Templates");
                await MessageBoxManager.GetMessageBoxStandard(
                    "Fehler",
                    $"Fehler beim Löschen: {ex.Message}",
                    ButtonEnum.Ok,
                    Icon.Error
                ).ShowAsync();
            }
        }
    }

    /// <summary>
    /// Rename selected template
    /// </summary>
    [RelayCommand]
    private async void RenameTemplate()
    {
        if (SelectedTemplate == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Kein Template ausgewählt",
                "Bitte wählen Sie ein Template zum Umbenennen aus.",
                ButtonEnum.Ok,
                Icon.Warning
            ).ShowAsync();
            return;
        }

        // Create input dialog
        var inputWindow = new Window
        {
            Title = "Template umbenennen",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stackPanel = new StackPanel { Margin = new(20) };
        var textBox = new TextBox { Text = SelectedTemplate.Name, Margin = new(0, 10) };
        var buttonsPanel = new StackPanel { Orientation = Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };

        var okButton = new Button { Content = "OK", Width = 80, Margin = new(5, 10, 0, 0) };
        var cancelButton = new Button { Content = "Abbrechen", Width = 80, Margin = new(5, 10, 0, 0) };

        bool confirmed = false;

        okButton.Click += (s, e) =>
        {
            confirmed = true;
            inputWindow.Close();
        };

        cancelButton.Click += (s, e) => inputWindow.Close();

        buttonsPanel.Children.Add(okButton);
        buttonsPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(new TextBlock { Text = "Neuer Name:" });
        stackPanel.Children.Add(textBox);
        stackPanel.Children.Add(buttonsPanel);

        inputWindow.Content = stackPanel;

        await inputWindow.ShowDialog((Window)App.MainWindow!);

        if (confirmed && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            try
            {
                _templateService.RenameTemplate(SelectedTemplate.Id, textBox.Text);
                LoadTemplates();

                await MessageBoxManager.GetMessageBoxStandard(
                    "Umbenannt",
                    "Template erfolgreich umbenannt.",
                    ButtonEnum.Ok,
                    Icon.Success
                ).ShowAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Umbenennen des Templates");
                await MessageBoxManager.GetMessageBoxStandard(
                    "Fehler",
                    $"Fehler beim Umbenennen: {ex.Message}",
                    ButtonEnum.Ok,
                    Icon.Error
                ).ShowAsync();
            }
        }
    }

    /// <summary>
    /// Apply selected template
    /// </summary>
    [RelayCommand]
    private void ApplyTemplate()
    {
        if (SelectedTemplate != null)
        {
            _onTemplateSelected?.Invoke(SelectedTemplate);
        }
    }

    /// <summary>
    /// Show template preview
    /// </summary>
    [RelayCommand]
    private async void ShowPreview()
    {
        if (SelectedTemplate == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Kein Template ausgewählt",
                "Bitte wählen Sie ein Template für die Vorschau aus.",
                ButtonEnum.Ok,
                Icon.Warning
            ).ShowAsync();
            return;
        }

        var preview = "📋 Template-Vorschau\n\n";
        preview += $"Name: {SelectedTemplate.Name}\n";
        preview += $"Kategorie: {SelectedTemplate.Category}\n";
        preview += $"Typ: {SelectedTemplate.Typ}\n";
        preview += $"Erstellt: {SelectedTemplate.CreatedAt:dd.MM.yyyy HH:mm}\n";
        preview += $"Quell-Monat: {SelectedTemplate.SourceMonth:MMMM yyyy}\n";
        preview += $"Anzahl Zuordnungen: {SelectedTemplate.AssignmentCount}\n\n";

        if (!string.IsNullOrWhiteSpace(SelectedTemplate.Description))
        {
            preview += $"Beschreibung:\n{SelectedTemplate.Description}\n\n";
        }

        preview += "Zuordnungen:\n";
        foreach (var kvp in SelectedTemplate.Assignments.OrderBy(x => x.Key))
        {
            var day = kvp.Key;
            var data = kvp.Value;
            preview += $"  Tag {day}: {data.RessourceName} ({data.GruppeName})\n";
        }

        await MessageBoxManager.GetMessageBoxStandard(
            "Template-Vorschau",
            preview,
            ButtonEnum.Ok,
            Icon.Info
        ).ShowAsync();
    }
}
