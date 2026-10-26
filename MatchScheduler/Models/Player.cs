using CathermanCore;

namespace MatchScheduler.Models;

public class Player : Entity
{
    public string Name { get; set; } = "";
    public Position DesiredPosition { get; set; } = Position.Any;
    public string PhoneNumber { get; set; } = "";
    public string Email { get; set; } = "";

    // Get-only, so EF does not map these. They exist for data binding.
    public string PositionLabel => PositionInfo.Label(DesiredPosition);
    public string PositionShort => PositionInfo.Short(DesiredPosition);

    public override bool IsValid() => !string.IsNullOrWhiteSpace(Name);
}
