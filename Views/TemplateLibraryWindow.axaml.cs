using Avalonia.Controls;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class TemplateLibraryWindow : Window
{
    public TemplateLibraryWindow()
    {
        InitializeComponent();
        DataContext = new TemplateLibraryViewModel();
    }

    public TemplateLibraryWindow(System.Action<Models.PlanningTemplate> onTemplateSelected)
    {
        InitializeComponent();
        DataContext = new TemplateLibraryViewModel(onTemplateSelected);
    }
}
