using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using MatchScheduler.Models;
using MatchScheduler.Services;

namespace MatchScheduler.ViewModels;

public class PlayersViewModel : ViewModelBase
{
    private readonly PlayerService _service = new();
    private string _searchText = "";
    private Player? _selectedPlayer;

    public PlayersViewModel()
    {
        PlayersView = CollectionViewSource.GetDefaultView(Players);
        PlayersView.SortDescriptions.Add(new SortDescription(nameof(Player.Name), ListSortDirection.Ascending));
        PlayersView.Filter = item => item is Player p && Matches(p);

        AddCommand = new AsyncRelayCommand(AddAsync);
        EditCommand = new AsyncRelayCommand(EditAsync, () => SelectedPlayer != null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedPlayer != null);
    }

    public ObservableCollection<Player> Players { get; } = [];
    public ICollectionView PlayersView { get; }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
                PlayersView.Refresh();
        }
    }

    public Player? SelectedPlayer
    {
        get => _selectedPlayer;
        set => Set(ref _selectedPlayer, value);
    }

    public string CountText => Players.Count == 1 ? "1 player" : $"{Players.Count} players";

    public async Task LoadAsync()
    {
        Players.Clear();

        foreach (var player in await _service.GetAllAsync())
            Players.Add(player);

        OnPropertyChanged(nameof(CountText));
    }

    private async Task AddAsync()
    {
        var entered = DialogService.EditPlayer(null);
        if (entered == null)
            return;

        var saved = await _service.AddAsync(entered);
        Players.Add(saved);
        SelectedPlayer = saved;
        OnPropertyChanged(nameof(CountText));
    }

    private async Task EditAsync()
    {
        if (SelectedPlayer == null)
            return;

        var entered = DialogService.EditPlayer(SelectedPlayer);
        if (entered == null)
            return;

        var saved = await _service.UpdateAsync(entered);
        var index = Players.IndexOf(SelectedPlayer);
        Players[index] = saved;
        SelectedPlayer = saved;
    }

    private async Task DeleteAsync()
    {
        if (SelectedPlayer == null)
            return;

        var player = SelectedPlayer;

        if (!DialogService.Confirm($"Remove {player.Name} from your player list?\n\nSchedules you already saved keep their name."))
            return;

        await _service.DeleteAsync(player.Id);
        Players.Remove(player);
        OnPropertyChanged(nameof(CountText));
    }

    private bool Matches(Player player)
    {
        var text = SearchText.Trim();
        if (text.Length == 0)
            return true;

        return player.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
            || player.Email.Contains(text, StringComparison.OrdinalIgnoreCase)
            || player.PhoneNumber.Contains(text, StringComparison.OrdinalIgnoreCase);
    }
}
