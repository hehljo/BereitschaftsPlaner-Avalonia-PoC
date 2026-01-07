using Avalonia.Controls;
using Avalonia.Interactivity;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class WelcomeWizardWindow : Window
{
    private readonly WelcomeWizardViewModel _viewModel;

    public bool WasCompleted { get; private set; } = false;

    public WelcomeWizardWindow()
    {
        InitializeComponent();
        _viewModel = new WelcomeWizardViewModel();
        DataContext = _viewModel;
    }

    private void NextButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsLastStep)
        {
            WasCompleted = true;
            Close();
        }
    }

    private void SkipButton_Click(object? sender, RoutedEventArgs e)
    {
        WasCompleted = false;
        Close();
    }
}
