namespace ClockShot.Core.Models;

/// <summary>
/// A span of UTC time that includes its start and ends just before its end.
/// Everything on the timeline is one of these.
/// </summary>
public readonly record struct TimeRange
{
    public TimeRange(DateTime start, DateTime end)
    {
        if (start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Time ranges are stored in UTC.");
        }

        if (end < start)
        {
            throw new ArgumentException("End must not be before start.", nameof(end));
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// From the earliest to the latest time .NET can hold, for when there's no limit.
    /// </summary>
    public static TimeRange All { get; } = new(
        DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
        DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));

    public DateTime Start { get; }
    public DateTime End { get; }
    public TimeSpan Duration => End - Start;

    public bool Overlaps(TimeRange other) => Start < other.End && other.Start < End;

    public bool Contains(DateTime time) => time >= Start && time < End;

    public bool Contains(TimeRange other) => other.Start >= Start && other.End <= End;

    public TimeRange? Intersect(TimeRange other)
    {
        var start = TimeMath.Max(Start, other.Start);
        var end = TimeMath.Min(End, other.End);
        return start < end ? new TimeRange(start, end) : null;
    }

    /// <summary>
    /// The smallest range that covers both ranges and any gap between them.
    /// </summary>
    public TimeRange Cover(TimeRange other) => new(TimeMath.Min(Start, other.Start), TimeMath.Max(End, other.End));
}
