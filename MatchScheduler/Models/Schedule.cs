using CathermanCore;

namespace MatchScheduler.Models;

public class Schedule : Entity
{
    public string Name { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int SlotMinutes { get; set; }
    public int CourtCount { get; set; }
    public int TeamSize { get; set; }
    public int RoundCount { get; set; }
    public int PlayerCount { get; set; }

    public List<ScheduleAssignment> Assignments { get; set; } = [];
}
