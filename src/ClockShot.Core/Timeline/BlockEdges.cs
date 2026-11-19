using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Finds the edges of blocks or ranges near the pointer, so a drag can snap to one or grab one.
/// </summary>
public static class BlockEdges
{
    /// <summary>
    /// The block start or end nearest the time, or the time itself when no edge is within the tolerance.
    /// </summary>
    public static DateTime Snap(IEnumerable<TimelineLane> lanes, DateTime time, TimeSpan tolerance) =>
        lanes.SelectMany(lane => lane.Blocks)
            .SelectMany(block => new[] { block.Range.Start, block.Range.End })
            .Where(edge => IsNear(edge, time, tolerance))
            .OrderBy(edge => (edge - time).Duration())
            .DefaultIfEmpty(time)
            .First();

    /// <summary>
    /// The end of the range that stays put while the end near the time is dragged.
    /// Returns null when neither end is within the tolerance.
    /// </summary>
    public static DateTime? AnchorNear(TimeRange range, DateTime time, TimeSpan tolerance)
    {
        if (IsNear(range.Start, time, tolerance))
        {
            return range.End;
        }

        if (IsNear(range.End, time, tolerance))
        {
            return range.Start;
        }

        return null;
    }

    private static bool IsNear(DateTime edge, DateTime time, TimeSpan tolerance) =>
        (edge - time).Duration() <= tolerance;
}
