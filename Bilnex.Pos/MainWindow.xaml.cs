using System.Windows;
using Bilnex.Pos.ViewModels;

namespace Bilnex.Pos;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
