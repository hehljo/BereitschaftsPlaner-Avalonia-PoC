using Avalonia.Controls;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class VacationCalendarWindow : Window
{
    public VacationCalendarWindow()
    {
        InitializeComponent();
        DataContext = new VacationCalendarViewModel();
    }
}
