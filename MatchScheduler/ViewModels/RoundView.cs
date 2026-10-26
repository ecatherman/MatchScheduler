using System.Text;
using MatchScheduler.Models;
using MatchScheduler.Services;

namespace MatchScheduler.ViewModels;

public class MatchView
{
    public string CourtLabel { get; set; } = "";
    public string TeamAText { get; set; } = "";
    public string TeamBText { get; set; } = "";
}

/// <summary>A round as shown on screen. Built from a fresh schedule or from a saved one.</summary>
public class RoundView
{
    public int Number { get; set; }
    public string TimeLabel { get; set; } = "";
    public List<MatchView> Matches { get; set; } = [];
    public string BenchText { get; set; } = "";

    public string Title => $"Round {Number}";
    public bool HasBench => BenchText.Length > 0;

    public static RoundView FromGenerated(GeneratedRound round) => new()
    {
        Number = round.Number,
        TimeLabel = TimeRange(round.Start, round.End),
        BenchText = string.Join(", ", round.Bench.Select(p => Label(p.Name, p.DesiredPosition))),
        Matches = round.Matches.Select(m => new MatchView
        {
            CourtLabel = $"Court {m.Court}",
            TeamAText = string.Join(", ", m.TeamA.Select(p => Label(p.Name, p.DesiredPosition))),
            TeamBText = string.Join(", ", m.TeamB.Select(p => Label(p.Name, p.DesiredPosition)))
        }).ToList()
    };

    public static List<RoundView> FromSaved(Schedule schedule, IEnumerable<ScheduleAssignment> assignments)
    {
        var views = new List<RoundView>();

        foreach (var roundGroup in assignments.GroupBy(a => a.RoundNumber).OrderBy(g => g.Key))
        {
            var start = schedule.StartsAt.AddMinutes((roundGroup.Key - 1) * schedule.SlotMinutes);

            var view = new RoundView
            {
                Number = roundGroup.Key,
                TimeLabel = TimeRange(start, start.AddMinutes(schedule.SlotMinutes)),
                BenchText = Join(roundGroup.Where(a => a.CourtNumber == 0))
            };

            foreach (var court in roundGroup.Where(a => a.CourtNumber > 0).GroupBy(a => a.CourtNumber).OrderBy(g => g.Key))
            {
                view.Matches.Add(new MatchView
                {
                    CourtLabel = $"Court {court.Key}",
                    TeamAText = Join(court.Where(a => a.Side == 1)),
                    TeamBText = Join(court.Where(a => a.Side == 2))
                });
            }

            views.Add(view);
        }

        return views;
    }

    private static string Join(IEnumerable<ScheduleAssignment> people) =>
        string.Join(", ", people.OrderBy(a => a.PlayerName).Select(a => Label(a.PlayerName, a.PlayerPosition)));

    private static string Label(string name, Position position)
    {
        var tag = PositionInfo.Short(position);
        return tag.Length == 0 ? name : $"{name} ({tag})";
    }

    private static string TimeRange(DateTime start, DateTime end) =>
        $"{start:h:mm tt} – {end:h:mm tt}";
}

public static class ScheduleText
{
    public static string Subtitle(Schedule s)
    {
        var courts = s.CourtCount == 1 ? "1 court" : $"{s.CourtCount} courts";
        return $"{s.StartsAt:dddd, MMM d} · {s.StartsAt:h:mm tt} – {s.EndsAt:h:mm tt} · {courts} · " +
               $"{s.SlotMinutes}-minute rounds · {s.PlayerCount} players · teams of {s.TeamSize}";
    }

    /// <summary>Plain text that pastes cleanly into a group chat or email.</summary>
    public static string Build(string title, string subtitle, IEnumerable<RoundView> rounds)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine(subtitle);
        sb.AppendLine();

        foreach (var round in rounds)
        {
            sb.AppendLine($"Round {round.Number} · {round.TimeLabel}");

            foreach (var match in round.Matches)
                sb.AppendLine($"  {match.CourtLabel}: {match.TeamAText}  vs  {match.TeamBText}");

            if (round.HasBench)
                sb.AppendLine($"  Sitting out: {round.BenchText}");

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
