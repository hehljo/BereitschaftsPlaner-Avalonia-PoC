using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class ImportPreviewWindow : Window
{
    public ImportPreviewWindow()
    {
        InitializeComponent();
    }

    public ImportPreviewWindow(ImportPreviewViewModel viewModel) : this()
    {
        DataContext = viewModel;

        // Subscribe to dialog result
        viewModel.DialogResultRequested += (confirmed) =>
        {
            Close(confirmed);
        };
    }
}
