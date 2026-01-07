using Avalonia.Controls;
using Avalonia.Interactivity;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class QuickStartGuideWindow : Window
{
    public QuickStartGuideWindow()
    {
        InitializeComponent();
        DataContext = new QuickStartGuideViewModel();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
