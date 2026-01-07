using Avalonia.Controls;
using BereitschaftsPlaner.Avalonia.ViewModels;

namespace BereitschaftsPlaner.Avalonia.Views;

public partial class PlanningBoardView : UserControl
{
    public PlanningBoardView()
    {
        InitializeComponent();
        DataContext = new PlanningBoardViewModel();
    }
}
