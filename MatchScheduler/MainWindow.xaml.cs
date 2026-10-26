using System.Windows;
using MatchScheduler.ViewModels;

namespace MatchScheduler;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
