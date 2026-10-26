using MatchScheduler.Models;

namespace MatchScheduler.Services;

/// <param name="Games">Games (courts in use) each round.</param>
/// <param name="PlayersOnCourt">Players playing each round. Everyone else sits out.</param>
/// <param name="MinTeamPlayers">Smallest team in a round.</param>
/// <param name="MaxTeamPlayers">Largest team in a round.</param>
public record RoundPlan(int Games, int PlayersOnCourt, int MinTeamPlayers, int MaxTeamPlayers);

public class GeneratedMatch
{
    public int Court { get; set; }
    public List<Player> TeamA { get; set; } = [];
    public List<Player> TeamB { get; set; } = [];
}

public class GeneratedRound
{
    public int Number { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<GeneratedMatch> Matches { get; set; } = [];
    public List<Player> Bench { get; set; } = [];
}

/// <summary>
/// Builds a rotating schedule. Pure logic: no database and no UI, so it is easy to test.
///
/// Each round:
///  1. Work out how many courts to use and how many players are on court (see <see cref="Plan"/>).
///  2. The players who have played the fewest rounds so far take the court; the rest sit out.
///     Ties are broken randomly, so sitting out rotates fairly.
///  3. Try many random ways to split the on-court players into teams. Each attempt spreads
///     desired positions evenly across teams. Keep the attempt that repeats the fewest teammates.
/// </summary>
public static class ScheduleGenerator
{
    private const int TeamAttemptsPerRound = 200;

    /// <summary>Teams may run one player short of the ideal size, never less than 2.</summary>
    public static int MinTeamSize(int teamSize) => Math.Max(2, teamSize - 1);

    public static int MinimumPlayers(int teamSize) => MinTeamSize(teamSize) * 2;

    public static int CountRounds(DateTime start, DateTime end, int slotMinutes) =>
        slotMinutes <= 0 ? 0 : Math.Max(0, (int)(end - start).TotalMinutes / slotMinutes);

    /// <summary>
    /// Picks the most courts we can fill while keeping every team at or above the minimum size.
    /// Returns null when there are not enough players for even one game.
    /// </summary>
    public static RoundPlan? Plan(int playerCount, int courtCount, int teamSize)
    {
        var minSize = MinTeamSize(teamSize);

        for (var games = courtCount; games >= 1; games--)
        {
            var teams = games * 2;
            var onCourt = Math.Min(playerCount, teams * teamSize);
            var smallest = onCourt / teams;

            if (smallest >= minSize)
                return new RoundPlan(games, onCourt, smallest, (onCourt + teams - 1) / teams);
        }

        return null;
    }

    public static List<GeneratedRound> Generate(
        IReadOnlyList<Player> players,
        int courtCount,
        int teamSize,
        int slotMinutes,
        DateTime start,
        DateTime end,
        Random? random = null)
    {
        var rnd = random ?? new Random();

        var plan = Plan(players.Count, courtCount, teamSize)
            ?? throw new InvalidOperationException(
                $"Teams of {teamSize} need at least {MinimumPlayers(teamSize)} players ({MinTeamSize(teamSize)} per side).");

        var roundCount = CountRounds(start, end, slotMinutes);
        if (roundCount < 1)
            throw new InvalidOperationException("The session is shorter than one time slot.");

        var n = players.Count;
        var played = new int[n];
        var together = new int[n, n];
        var rounds = new List<GeneratedRound>();

        for (var r = 0; r < roundCount; r++)
        {
            var order = Enumerable.Range(0, n)
                .OrderBy(i => played[i])
                .ThenBy(_ => rnd.Next())
                .ToList();

            var active = order.Take(plan.PlayersOnCourt).ToList();
            var bench = order.Skip(plan.PlayersOnCourt).ToList();

            var teams = BestTeams(active, plan.Games * 2, players, together, rnd);

            foreach (var i in active)
                played[i]++;

            foreach (var team in teams)
                for (var a = 0; a < team.Count; a++)
                    for (var b = a + 1; b < team.Count; b++)
                    {
                        together[team[a], team[b]]++;
                        together[team[b], team[a]]++;
                    }

            var roundStart = start.AddMinutes(r * slotMinutes);
            var round = new GeneratedRound
            {
                Number = r + 1,
                Start = roundStart,
                End = roundStart.AddMinutes(slotMinutes),
                Bench = bench.Select(i => players[i]).OrderBy(p => p.Name).ToList()
            };

            for (var g = 0; g < plan.Games; g++)
            {
                round.Matches.Add(new GeneratedMatch
                {
                    Court = g + 1,
                    TeamA = teams[g * 2].Select(i => players[i]).OrderBy(p => p.Name).ToList(),
                    TeamB = teams[g * 2 + 1].Select(i => players[i]).OrderBy(p => p.Name).ToList()
                });
            }

            rounds.Add(round);
        }

        return rounds;
    }

    /// <summary>How many rounds each player is on court for.</summary>
    public static Dictionary<Player, int> CountGames(IEnumerable<GeneratedRound> rounds, IEnumerable<Player> players)
    {
        var counts = players.ToDictionary(p => p, _ => 0);

        foreach (var round in rounds)
            foreach (var match in round.Matches)
                foreach (var p in match.TeamA.Concat(match.TeamB))
                    counts[p]++;

        return counts;
    }

    private static List<List<int>> BestTeams(
        List<int> active, int teamCount, IReadOnlyList<Player> players, int[,] together, Random rnd)
    {
        List<List<int>>? best = null;
        var bestScore = int.MaxValue;

        for (var attempt = 0; attempt < TeamAttemptsPerRound; attempt++)
        {
            var teams = DealTeams(active, teamCount, players, rnd);
            var score = RepeatTeammateScore(teams, together);

            if (score < bestScore)
            {
                best = teams;
                bestScore = score;
                if (score == 0)
                    break;
            }
        }

        return best!;
    }

    private static int RepeatTeammateScore(List<List<int>> teams, int[,] together)
    {
        var score = 0;

        foreach (var team in teams)
            for (var a = 0; a < team.Count; a++)
                for (var b = a + 1; b < team.Count; b++)
                    score += together[team[a], team[b]];

        return score;
    }

    /// <summary>
    /// One random split into teams. Players with a stated position go first, each to the team
    /// that has the fewest of that position so far. "No preference" players fill whatever room is left.
    /// </summary>
    private static List<List<int>> DealTeams(List<int> active, int teamCount, IReadOnlyList<Player> players, Random rnd)
    {
        var capacity = new int[teamCount];
        for (var t = 0; t < teamCount; t++)
            capacity[t] = active.Count / teamCount + (t < active.Count % teamCount ? 1 : 0);

        var teams = Enumerable.Range(0, teamCount).Select(_ => new List<int>()).ToList();

        var shuffled = active.OrderBy(_ => rnd.Next()).ToList();
        var specific = shuffled
            .Where(i => players[i].DesiredPosition != Position.Any)
            .OrderBy(i => (int)players[i].DesiredPosition)
            .ToList();
        var flexible = shuffled.Where(i => players[i].DesiredPosition == Position.Any);

        foreach (var i in specific.Concat(flexible))
        {
            var position = players[i].DesiredPosition;
            var choices = new List<int>();
            var fewestSamePosition = int.MaxValue;
            var smallestTeam = int.MaxValue;

            for (var t = 0; t < teamCount; t++)
            {
                if (teams[t].Count >= capacity[t])
                    continue;

                var samePosition = position == Position.Any
                    ? 0
                    : teams[t].Count(m => players[m].DesiredPosition == position);
                var size = teams[t].Count;

                if (samePosition < fewestSamePosition || (samePosition == fewestSamePosition && size < smallestTeam))
                {
                    choices.Clear();
                    fewestSamePosition = samePosition;
                    smallestTeam = size;
                    choices.Add(t);
                }
                else if (samePosition == fewestSamePosition && size == smallestTeam)
                {
                    choices.Add(t);
                }
            }

            teams[choices[rnd.Next(choices.Count)]].Add(i);
        }

        return teams;
    }
}
