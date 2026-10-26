namespace MatchScheduler.Models;

public enum Position
{
    Any = 0,
    Setter,
    OutsideHitter,
    OppositeHitter,
    MiddleBlocker,
    Libero,
    DefensiveSpecialist
}

public record PositionItem(Position Value, string Label);

public static class PositionInfo
{
    public static IReadOnlyList<PositionItem> All { get; } =
        Enum.GetValues<Position>().Select(p => new PositionItem(p, Label(p))).ToList();

    public static string Label(Position position) => position switch
    {
        Position.Any => "No preference",
        Position.Setter => "Setter",
        Position.OutsideHitter => "Outside hitter",
        Position.OppositeHitter => "Opposite hitter",
        Position.MiddleBlocker => "Middle blocker",
        Position.Libero => "Libero",
        Position.DefensiveSpecialist => "Defensive specialist",
        _ => position.ToString()
    };

    /// <summary>Short tag shown next to a name on the schedule. Empty for "No preference".</summary>
    public static string Short(Position position) => position switch
    {
        Position.Setter => "S",
        Position.OutsideHitter => "OH",
        Position.OppositeHitter => "OPP",
        Position.MiddleBlocker => "MB",
        Position.Libero => "L",
        Position.DefensiveSpecialist => "DS",
        _ => ""
    };
}
