using Avalonia.Controls;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}
