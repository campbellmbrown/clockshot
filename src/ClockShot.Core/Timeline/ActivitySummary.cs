using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Totals over a time range, such as how long each application was used and how long the computer was active.
/// </summary>
public static class ActivitySummary
{
    /// <summary>
    /// The time each application was used within <paramref name="range"/>, longest first.
    /// </summary>
    public static IReadOnlyList<ApplicationTotal> ByApplication(
        IEnumerable<ActivitySegment> activities,
        TimeRange range) =>
        [.. activities
            .Select(a => (a.ApplicationId, Overlap: a.Range.Intersect(range)))
            .Where(a => a.Overlap is not null)
            .GroupBy(a => a.ApplicationId, a => a.Overlap!.Value.Duration)
            .Select(g => new ApplicationTotal(g.Key, g.Sum()))
            .OrderByDescending(t => t.Duration)
            .ThenBy(t => t.ApplicationId)];

    /// <summary>
    /// The total time in <paramref name="state"/> within <paramref name="range"/>.
    /// </summary>
    public static TimeSpan TimeIn(IEnumerable<ComputerStateSegment> states, ComputerState state, TimeRange range) =>
        states.Where(s => s.State == state)
            .Select(s => s.Range.Intersect(range)?.Duration ?? TimeSpan.Zero)
            .Sum();

    /// <summary>
    /// The working day within <paramref name="range"/>, from the start of the first active time to the end of the last.
    /// It's null when there was no active time.
    /// </summary>
    public static TimeRange? WorkingDay(IEnumerable<ComputerStateSegment> states, TimeRange range)
    {
        var active = states
            .Where(s => s.State == ComputerState.Active)
            .Select(s => s.Range.Intersect(range))
            .OfType<TimeRange>()
            .ToList();
        return active.Count == 0 ? null : new TimeRange(active.Min(r => r.Start), active.Max(r => r.End));
    }
}
