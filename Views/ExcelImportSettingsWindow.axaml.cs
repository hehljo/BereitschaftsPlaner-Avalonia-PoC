using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class ExcelImportSettingsWindow : Window
{
    public ExcelImportSettingsWindow()
    {
        InitializeComponent();

        var viewModel = new ExcelImportSettingsViewModel();
        viewModel.CloseRequested += () => Close();
        DataContext = viewModel;
    }
}
