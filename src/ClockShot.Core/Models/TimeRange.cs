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

    public DateTime Start { get; }
    public DateTime End { get; }
}
