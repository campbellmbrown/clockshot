namespace ClockShot.Core.Timeline;

/// <summary>
/// Finds the edges of blocks near the pointer, so a drag can snap to one.
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

    private static bool IsNear(DateTime edge, DateTime time, TimeSpan tolerance) =>
        (edge - time).Duration() <= tolerance;
}
