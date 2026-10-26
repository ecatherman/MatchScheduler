using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MatchScheduler.Models;
using MatchScheduler.Services;

namespace MatchScheduler.ViewModels;

public class SavedSchedulesViewModel : ViewModelBase
{
    private readonly ScheduleService _service = new();
    private Schedule? _selectedSchedule;
    private IReadOnlyList<RoundView> _rounds = [];
    private string _status = "";

    public SavedSchedulesViewModel()
    {
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedSchedule != null);
        CopyTextCommand = new RelayCommand(CopyText, () => SelectedSchedule != null);
    }

    public ObservableCollection<Schedule> Schedules { get; } = [];

    public ICommand DeleteCommand { get; }
    public ICommand CopyTextCommand { get; }

    public bool HasSchedules => Schedules.Count > 0;
    public bool HasNoSchedules => Schedules.Count == 0;

    public IReadOnlyList<RoundView> Rounds
    {
        get => _rounds;
        private set => Set(ref _rounds, value);
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public string Subtitle => SelectedSchedule == null ? "" : ScheduleText.Subtitle(SelectedSchedule);

    public Schedule? SelectedSchedule
    {
        get => _selectedSchedule;
        set
        {
            if (!Set(ref _selectedSchedule, value))
                return;

            OnPropertyChanged(nameof(Subtitle));
            Status = "";
            Run(ShowSelectedAsync);
        }
    }

    public async Task LoadAsync(int? selectId = null)
    {
        Schedules.Clear();

        foreach (var schedule in await _service.GetAllAsync())
            Schedules.Add(schedule);

        OnPropertyChanged(nameof(HasSchedules));
        OnPropertyChanged(nameof(HasNoSchedules));

        SelectedSchedule = Schedules.FirstOrDefault(s => s.Id == selectId) ?? Schedules.FirstOrDefault();
    }

    private async Task ShowSelectedAsync()
    {
        var schedule = SelectedSchedule;

        if (schedule == null)
        {
            Rounds = [];
            return;
        }

        var assignments = await _service.GetAssignmentsAsync(schedule.Id);

        // The user may have picked a different schedule while this one was loading.
        if (schedule == SelectedSchedule)
            Rounds = RoundView.FromSaved(schedule, assignments);
    }

    private async Task DeleteAsync()
    {
        var schedule = SelectedSchedule;
        if (schedule == null)
            return;

        if (!DialogService.Confirm($"Delete \"{schedule.Name}\"?"))
            return;

        await _service.DeleteAsync(schedule.Id);

        var index = Schedules.IndexOf(schedule);
        Schedules.Remove(schedule);

        OnPropertyChanged(nameof(HasSchedules));
        OnPropertyChanged(nameof(HasNoSchedules));

        SelectedSchedule = Schedules.Count == 0 ? null : Schedules[Math.Min(index, Schedules.Count - 1)];
    }

    private void CopyText()
    {
        if (SelectedSchedule == null)
            return;

        var text = ScheduleText.Build(SelectedSchedule.Name, ScheduleText.Subtitle(SelectedSchedule), Rounds);

        try
        {
            Clipboard.SetText(text);
            Status = "Copied. Paste it into your group chat.";
        }
        catch (Exception ex)
        {
            Status = "Couldn't copy: " + ex.Message;
        }
    }
}
