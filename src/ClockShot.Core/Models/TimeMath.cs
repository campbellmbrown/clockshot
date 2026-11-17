namespace ClockShot.Core.Models;

/// <summary>
/// Small time helpers that LINQ and <see cref="DateTime"/> don't provide.
/// </summary>
public static class TimeMath
{
    public static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    public static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    /// <summary>
    /// The total of the durations, which is zero when there are none.
    /// </summary>
    public static TimeSpan Sum(this IEnumerable<TimeSpan> durations) =>
        durations.Aggregate(TimeSpan.Zero, (sum, d) => sum + d);
}
