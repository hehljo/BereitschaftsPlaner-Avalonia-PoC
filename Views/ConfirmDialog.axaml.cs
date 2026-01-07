using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class ConfirmDialog : Window
{
    public new string Title { get; set; } = "Bestätigung";
    public string Message { get; set; } = "Möchten Sie fortfahren?";
    public string ConfirmText { get; set; } = "OK";
    public string CancelText { get; set; } = "Abbrechen";

    public ConfirmDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    public ConfirmDialog(string title, string message, string confirmText, string cancelText) : this()
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
    }

    private void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
