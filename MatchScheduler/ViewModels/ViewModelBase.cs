using System.ComponentModel;
using System.Runtime.CompilerServices;
using MatchScheduler.Services;

namespace MatchScheduler.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>Starts async work from a place that cannot await, showing any error instead of losing it.</summary>
    protected static async void Run(Func<Task> work)
    {
        try
        { await work(); }
        catch (Exception ex)
        { DialogService.ShowError(ex); }
    }
}
