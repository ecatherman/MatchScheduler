using MatchScheduler.Models;

namespace MatchScheduler.ViewModels;

/// <summary>A player plus a checked/unchecked state, for the wizard's player picker.</summary>
public class SelectablePlayer(Player player) : ViewModelBase
{
    private bool _isSelected;

    public Player Player { get; } = player;

    public string Name => Player.Name;
    public string PositionShort => Player.PositionShort;
    public string ToolTipText
    {
        get
        {
            var parts = new List<string> { Player.PositionLabel };
            if (Player.PhoneNumber.Length > 0) parts.Add(Player.PhoneNumber);
            if (Player.Email.Length > 0) parts.Add(Player.Email);
            return string.Join("\n", parts);
        }
    }

    public event Action? SelectionChanged;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (Set(ref _isSelected, value))
                SelectionChanged?.Invoke();
        }
    }
}
