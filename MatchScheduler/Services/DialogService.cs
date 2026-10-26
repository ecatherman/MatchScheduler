using System.Windows;
using MatchScheduler.Models;
using MatchScheduler.ViewModels;
using MatchScheduler.Views;
using Serilog;

namespace MatchScheduler.Services;

/// <summary>The few places view models need a window or a message box.</summary>
public static class DialogService
{
    private static Window? Owner => Application.Current?.MainWindow;

    /// <summary>Opens the add/edit window. Returns the entered values, or null if cancelled.</summary>
    public static Player? EditPlayer(Player? existing)
    {
        var vm = new PlayerEditViewModel(existing);
        var window = new PlayerEditWindow { DataContext = vm };

        if (Owner != null && Owner.IsVisible)
            window.Owner = Owner;

        return window.ShowDialog() == true ? vm.ToPlayer() : null;
    }

    public static bool Confirm(string message, string title = "Please confirm") =>
        Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static void ShowError(Exception ex)
    {
        Log.Error(ex, "Unhandled error");
        Show(ex.Message, "Something went wrong", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage icon) =>
        Owner != null
            ? MessageBox.Show(Owner, message, title, buttons, icon)
            : MessageBox.Show(message, title, buttons, icon);
}
