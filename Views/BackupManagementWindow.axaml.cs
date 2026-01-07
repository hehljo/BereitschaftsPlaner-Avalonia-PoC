using Avalonia.Controls;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class BackupManagementWindow : Window
{
    public BackupManagementWindow()
    {
        InitializeComponent();
        DataContext = new BackupManagementViewModel(this);
    }
}
