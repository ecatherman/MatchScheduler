using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MatchScheduler.Models;
using MatchScheduler.Services;

namespace MatchScheduler.ViewModels;

public record TimeOption(TimeSpan Value, string Label);

/// <summary>
/// Three steps: 1) session setup, 2) pick players (with quick add), 3) review, regenerate, save.
/// </summary>
public class ScheduleWizardViewModel : ViewModelBase
{
    private readonly PlayerService _playerService = new();
    private readonly ScheduleService _scheduleService = new();
    private readonly Action<int> _onSaved;

    private int _step = 1;
    private string _errorMessage = "";

    // Step 1
    private string _scheduleName = "Volleyball night";
    private DateTime _eventDate = DateTime.Today;
    private TimeSpan _startTime = TimeSpan.FromHours(18);
    private TimeSpan _endTime = TimeSpan.FromHours(20);
    private int _slotMinutes = 15;
    private int _courtCount = 2;
    private int _teamSize = 6;

    // Step 2
    private string _searchText = "";
    private string _quickAddName = "";
    private Position _quickAddPosition = Position.Any;
    private string _playerStatus = "";

    // Step 3
    private List<GeneratedRound> _generated = [];
    private IReadOnlyList<RoundView> _rounds = [];
    private string _resultSummary = "";
    private string _resultStatus = "";

    public ScheduleWizardViewModel(Action<int> onSaved)
    {
        _onSaved = onSaved;

        PlayersView = CollectionViewSource.GetDefaultView(Players);
        PlayersView.SortDescriptions.Add(new SortDescription(nameof(SelectablePlayer.Name), ListSortDirection.Ascending));
        PlayersView.Filter = item => item is SelectablePlayer p && MatchesSearch(p);

        NextCommand = new RelayCommand(Next);
        BackCommand = new RelayCommand(Back);
        SelectAllCommand = new RelayCommand(SelectAllVisible);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        QuickAddCommand = new AsyncRelayCommand(QuickAddAsync);
        AddWithDetailsCommand = new AsyncRelayCommand(AddWithDetailsAsync);
        RegenerateCommand = new RelayCommand(Generate);
        CopyTextCommand = new RelayCommand(CopyText);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    #region Commands and option lists

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand QuickAddCommand { get; }
    public ICommand AddWithDetailsCommand { get; }
    public ICommand RegenerateCommand { get; }
    public ICommand CopyTextCommand { get; }
    public ICommand SaveCommand { get; }

    // Quarter-hour choices from 6:00 AM to 11:45 PM.
    public IReadOnlyList<TimeOption> TimeOptions { get; } = Enumerable.Range(24, 72)
        .Select(q => TimeSpan.FromMinutes(q * 15))
        .Select(t => new TimeOption(t, (DateTime.Today + t).ToString("h:mm tt")))
        .ToList();

    public IReadOnlyList<int> SlotOptions { get; } = [5, 8, 10, 12, 15, 20, 25, 30, 40, 45, 60, 90];
    public IReadOnlyList<int> CourtOptions { get; } = Enumerable.Range(1, 12).ToList();
    public IReadOnlyList<int> TeamSizeOptions { get; } = Enumerable.Range(2, 8).ToList();
    public IReadOnlyList<PositionItem> PositionOptions => PositionInfo.All;

    #endregion

    #region Navigation

    public int Step
    {
        get => _step;
        private set
        {
            if (!Set(ref _step, value))
                return;

            OnPropertyChanged(nameof(IsStep1));
            OnPropertyChanged(nameof(IsStep2));
            OnPropertyChanged(nameof(IsStep3));
            OnPropertyChanged(nameof(ShowBack));
            OnPropertyChanged(nameof(ShowNext));
            OnPropertyChanged(nameof(NextLabel));
            OnPropertyChanged(nameof(StepTitle));
        }
    }

    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;
    public bool ShowBack => Step > 1;
    public bool ShowNext => Step < 3;
    public string NextLabel => Step == 1 ? "Next: choose players" : "Next: build schedule";

    public string StepTitle => Step switch
    {
        1 => "Step 1 of 3 · Session setup",
        2 => "Step 2 of 3 · Who's playing?",
        _ => "Step 3 of 3 · Your schedule"
    };

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (Set(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => ErrorMessage.Length > 0;

    private void Next()
    {
        ErrorMessage = "";

        if (Step == 1)
        {
            var problem = ValidateSetup();
            if (problem != null)
            {
                ErrorMessage = problem;
                return;
            }

            Step = 2;
        }
        else if (Step == 2)
        {
            var problem = ValidatePlayers();
            if (problem != null)
            {
                ErrorMessage = problem;
                return;
            }

            Generate();
            if (!HasError)
                Step = 3;
        }
    }

    private void Back()
    {
        ErrorMessage = "";

        if (Step > 1)
            Step--;
    }

    private string? ValidateSetup()
    {
        if (SessionEnd <= SessionStart)
            return "The end time needs to be after the start time.";

        if (ScheduleGenerator.CountRounds(SessionStart, SessionEnd, SlotMinutes) < 1)
            return "The session is shorter than one time slot. Pick a shorter slot or a later end time.";

        return null;
    }

    private string? ValidatePlayers()
    {
        if (ScheduleGenerator.Plan(SelectedCount, CourtCount, TeamSize) != null)
            return null;

        return $"Teams of {TeamSize} need at least {ScheduleGenerator.MinimumPlayers(TeamSize)} players and you've picked " +
               $"{SelectedCount}. Pick more players, or go back and lower the team size.";
    }

    #endregion

    #region Step 1: setup

    public string ScheduleName { get => _scheduleName; set => Set(ref _scheduleName, value); }

    public DateTime EventDate
    {
        get => _eventDate;
        set { if (Set(ref _eventDate, value)) SetupChanged(); }
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set { if (Set(ref _startTime, value)) SetupChanged(); }
    }

    public TimeSpan EndTime
    {
        get => _endTime;
        set { if (Set(ref _endTime, value)) SetupChanged(); }
    }

    public int SlotMinutes
    {
        get => _slotMinutes;
        set { if (Set(ref _slotMinutes, value)) SetupChanged(); }
    }

    public int CourtCount
    {
        get => _courtCount;
        set { if (Set(ref _courtCount, value)) SetupChanged(); }
    }

    public int TeamSize
    {
        get => _teamSize;
        set { if (Set(ref _teamSize, value)) SetupChanged(); }
    }

    private DateTime SessionStart => EventDate.Date + StartTime;
    private DateTime SessionEnd => EventDate.Date + EndTime;

    public string SetupSummary
    {
        get
        {
            if (SessionEnd <= SessionStart)
                return "The end time needs to be after the start time.";

            var rounds = ScheduleGenerator.CountRounds(SessionStart, SessionEnd, SlotMinutes);
            if (rounds < 1)
                return "That session is shorter than one time slot.";

            var text = $"{rounds} rounds of {SlotMinutes} minutes · up to {CourtCount * 2 * TeamSize} players on court at once";

            var leftover = (int)(SessionEnd - SessionStart).TotalMinutes - rounds * SlotMinutes;
            if (leftover > 0)
                text += $" · {leftover} min left over at the end";

            return text;
        }
    }

    private void SetupChanged()
    {
        OnPropertyChanged(nameof(SetupSummary));
        OnPropertyChanged(nameof(FitSummary));
    }

    #endregion

    #region Step 2: players

    public ObservableCollection<SelectablePlayer> Players { get; } = [];
    public ICollectionView PlayersView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
                PlayersView.Refresh();
        }
    }

    public string QuickAddName { get => _quickAddName; set => Set(ref _quickAddName, value); }
    public Position QuickAddPosition { get => _quickAddPosition; set => Set(ref _quickAddPosition, value); }
    public string PlayerStatus { get => _playerStatus; private set => Set(ref _playerStatus, value); }

    public int SelectedCount => Players.Count(p => p.IsSelected);

    /// <summary>Live answer to "will this work?" while the organizer ticks players.</summary>
    public string FitSummary
    {
        get
        {
            var count = SelectedCount;
            if (count == 0)
                return "Tap the players who are coming.";

            var plan = ScheduleGenerator.Plan(count, CourtCount, TeamSize);
            if (plan == null)
                return $"{count} selected. Teams of {TeamSize} need at least {ScheduleGenerator.MinimumPlayers(TeamSize)} players.";

            var teamText = plan.MinTeamPlayers == plan.MaxTeamPlayers
                ? $"{plan.MinTeamPlayers}"
                : $"{plan.MinTeamPlayers}–{plan.MaxTeamPlayers}";

            var text = $"{count} selected · {plan.Games} {(plan.Games == 1 ? "game" : "games")} per round · {teamText} players per team";

            var sitting = count - plan.PlayersOnCourt;
            if (sitting > 0)
                text += $" · {sitting} sit out each round (rotating)";

            return text;
        }
    }

    public async Task LoadAsync()
    {
        foreach (var player in await _playerService.GetAllAsync())
            AddSelectable(player, selected: false);

        PlayerStatus = Players.Count == 0 ? "No players yet. Add your first one below." : "";
        UpdateSelection();
    }

    private SelectablePlayer AddSelectable(Player player, bool selected)
    {
        var item = new SelectablePlayer(player);
        item.SelectionChanged += UpdateSelection;
        Players.Add(item);

        if (selected)
            item.IsSelected = true;

        return item;
    }

    private void UpdateSelection()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(FitSummary));
    }

    private bool MatchesSearch(SelectablePlayer player)
    {
        var text = SearchText.Trim();
        return text.Length == 0 || player.Name.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private void SelectAllVisible()
    {
        foreach (var player in PlayersView.Cast<SelectablePlayer>())
            player.IsSelected = true;
    }

    private void ClearSelection()
    {
        foreach (var player in Players)
            player.IsSelected = false;
    }

    /// <summary>Type a name, press Enter: the player is saved and ticked. An existing name is just ticked.</summary>
    private async Task QuickAddAsync()
    {
        var name = QuickAddName.Trim();
        if (name.Length == 0)
            return;

        var existing = Players.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.IsSelected = true;
            PlayerStatus = $"{existing.Name} is already in your list. Selected.";
        }
        else
        {
            var saved = await _playerService.AddAsync(new Player { Name = name, DesiredPosition = QuickAddPosition });
            AddSelectable(saved, selected: true);
            PlayerStatus = $"Added {saved.Name}.";
        }

        QuickAddName = "";
        QuickAddPosition = Position.Any;
    }

    /// <summary>For when you also want a phone number or email.</summary>
    private async Task AddWithDetailsAsync()
    {
        var entered = DialogService.EditPlayer(null);
        if (entered == null)
            return;

        var saved = await _playerService.AddAsync(entered);
        AddSelectable(saved, selected: true);
        PlayerStatus = $"Added {saved.Name}.";
    }

    #endregion

    #region Step 3: result

    public IReadOnlyList<RoundView> Rounds
    {
        get => _rounds;
        private set => Set(ref _rounds, value);
    }

    public string ResultSummary { get => _resultSummary; private set => Set(ref _resultSummary, value); }
    public string ResultStatus { get => _resultStatus; private set => Set(ref _resultStatus, value); }

    private void Generate()
    {
        try
        {
            var chosen = Players.Where(p => p.IsSelected).Select(p => p.Player).ToList();

            _generated = ScheduleGenerator.Generate(chosen, CourtCount, TeamSize, SlotMinutes, SessionStart, SessionEnd);
            Rounds = _generated.Select(RoundView.FromGenerated).ToList();

            var plan = ScheduleGenerator.Plan(chosen.Count, CourtCount, TeamSize)!;
            var counts = ScheduleGenerator.CountGames(_generated, chosen).Values;
            var min = counts.Min();
            var max = counts.Max();

            var text = $"{_generated.Count} rounds · {plan.Games} {(plan.Games == 1 ? "game" : "games")} per round · " +
                       (min == max ? $"everyone plays {min} rounds" : $"everyone plays {min}–{max} rounds");

            var sitting = chosen.Count - plan.PlayersOnCourt;
            if (sitting > 0)
                text += $" · {sitting} sit out each round";

            ResultSummary = text;
            ResultStatus = "";
            ErrorMessage = "";
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private Schedule BuildSchedule() => new()
    {
        Name = string.IsNullOrWhiteSpace(ScheduleName) ? "Volleyball" : ScheduleName.Trim(),
        StartsAt = SessionStart,
        EndsAt = SessionEnd,
        SlotMinutes = SlotMinutes,
        CourtCount = CourtCount,
        TeamSize = TeamSize,
        RoundCount = _generated.Count,
        PlayerCount = SelectedCount
    };

    private void CopyText()
    {
        var schedule = BuildSchedule();
        var text = ScheduleText.Build(schedule.Name, ScheduleText.Subtitle(schedule), Rounds);

        try
        {
            Clipboard.SetText(text);
            ResultStatus = "Copied. Paste it into your group chat.";
        }
        catch (Exception ex)
        {
            ResultStatus = "Couldn't copy: " + ex.Message;
        }
    }

    private async Task SaveAsync()
    {
        var saved = await _scheduleService.SaveAsync(BuildSchedule(), _generated);
        _onSaved(saved.Id);
    }

    #endregion
}
