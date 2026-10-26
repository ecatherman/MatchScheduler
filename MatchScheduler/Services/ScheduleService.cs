using MatchScheduler.Data;
using MatchScheduler.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchScheduler.Services;

public class ScheduleService
{
    public async Task<Schedule> SaveAsync(Schedule schedule, IEnumerable<GeneratedRound> rounds)
    {
        foreach (var round in rounds)
        {
            foreach (var match in round.Matches)
            {
                foreach (var player in match.TeamA)
                    schedule.Assignments.Add(NewAssignment(round.Number, match.Court, 1, player));

                foreach (var player in match.TeamB)
                    schedule.Assignments.Add(NewAssignment(round.Number, match.Court, 2, player));
            }

            foreach (var player in round.Bench)
                schedule.Assignments.Add(NewAssignment(round.Number, 0, 0, player));
        }

        using var db = AppDatabase.CreateContext();
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule;
    }

    public async Task<List<Schedule>> GetAllAsync()
    {
        using var db = AppDatabase.CreateContext();
        return await db.Schedules.AsNoTracking().OrderByDescending(s => s.StartsAt).ToListAsync();
    }

    public async Task<List<ScheduleAssignment>> GetAssignmentsAsync(int scheduleId)
    {
        using var db = AppDatabase.CreateContext();
        return await db.ScheduleAssignments.AsNoTracking()
            .Where(a => a.ScheduleId == scheduleId)
            .ToListAsync();
    }

    public async Task DeleteAsync(int scheduleId)
    {
        using var db = AppDatabase.CreateContext();
        var schedule = await db.Schedules
            .Include(s => s.Assignments)
            .SingleOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null)
            return;

        foreach (var assignment in schedule.Assignments)
            assignment.IsLive = false;

        schedule.IsLive = false;
        await db.SaveChangesAsync();
    }

    private static ScheduleAssignment NewAssignment(int round, int court, int side, Player player) => new()
    {
        RoundNumber = round,
        CourtNumber = court,
        Side = side,
        PlayerId = player.Id,
        PlayerName = player.Name,
        PlayerPosition = player.DesiredPosition
    };
}
