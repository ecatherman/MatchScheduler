using System.Windows.Input;

namespace MatchScheduler.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase? _currentPage;
    private string _currentSection = "";

    public MainViewModel()
    {
        ShowWizardCommand = new RelayCommand(ShowWizard);
        ShowSavedCommand = new RelayCommand(() => ShowSaved());
        ShowPlayersCommand = new RelayCommand(ShowPlayers);

        ShowWizard();
    }

    public ICommand ShowWizardCommand { get; }
    public ICommand ShowSavedCommand { get; }
    public ICommand ShowPlayersCommand { get; }

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        private set => Set(ref _currentPage, value);
    }

    /// <summary>"wizard", "saved" or "players". The sidebar uses it to highlight the open page.</summary>
    public string CurrentSection
    {
        get => _currentSection;
        private set => Set(ref _currentSection, value);
    }

    private void ShowWizard()
    {
        var page = new ScheduleWizardViewModel(savedId => ShowSaved(savedId));
        CurrentSection = "wizard";
        CurrentPage = page;
        Run(page.LoadAsync);
    }

    private void ShowSaved(int? selectId = null)
    {
        var page = new SavedSchedulesViewModel();
        CurrentSection = "saved";
        CurrentPage = page;
        Run(() => page.LoadAsync(selectId));
    }

    private void ShowPlayers()
    {
        var page = new PlayersViewModel();
        CurrentSection = "players";
        CurrentPage = page;
        Run(page.LoadAsync);
    }
}
