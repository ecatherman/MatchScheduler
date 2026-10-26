using CathermanCore;

namespace MatchScheduler.Models;

/// <summary>
/// One player's place in one round of a saved schedule.
/// CourtNumber 0 (and Side 0) means the player is sitting out that round.
/// The name and position are copied here on purpose, so a saved schedule still reads
/// correctly after a player is renamed or deleted.
/// </summary>
public class ScheduleAssignment : Entity
{
    public int ScheduleId { get; set; }
    public Schedule Schedule { get; set; } = null!;

    public int RoundNumber { get; set; }
    public int CourtNumber { get; set; }
    public int Side { get; set; }

    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public Position PlayerPosition { get; set; } = Position.Any;
}
