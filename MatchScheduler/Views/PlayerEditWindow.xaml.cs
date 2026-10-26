using System.Windows;
using MatchScheduler.ViewModels;

namespace MatchScheduler.Views;

public partial class PlayerEditWindow : Window
{
    public PlayerEditWindow()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Validate() fills in the error message the window shows when something is missing.
        if (DataContext is PlayerEditViewModel vm && vm.Validate())
            DialogResult = true;
    }
}
