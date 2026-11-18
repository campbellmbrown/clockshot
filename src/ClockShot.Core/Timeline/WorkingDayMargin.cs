using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// The space shown either side of the working day when the timeline zooms to it.
/// It keeps the first and last active blocks away from the edges, so they're easy to see.
/// </summary>
public static class WorkingDayMargin
{
    /// <summary>
    /// On a 10-hour working day this is 12 minutes, which clears the edges without wasting much width.
    /// </summary>
    private const double FractionOfTheWorkingDay = 0.02;

    /// <summary>
    /// A working day shorter than <see cref="TimelineViewport.MinVisibleDuration"/> would otherwise sit at the left edge
    /// instead of between margins.
    /// </summary>
    private static readonly TimeSpan Minimum = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The working day with a margin either side, kept inside the day.
    /// </summary>
    public static TimeRange Around(TimeRange workingDay, TimeRange day)
    {
        var margin = workingDay.Duration * FractionOfTheWorkingDay;
        if (margin < Minimum)
        {
            margin = Minimum;
        }

        return new TimeRange(
            TimeMath.Max(day.Start, workingDay.Start - margin),
            TimeMath.Min(day.End, workingDay.End + margin));
    }
}
