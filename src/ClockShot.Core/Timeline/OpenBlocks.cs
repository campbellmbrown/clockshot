using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// The newest state and window are still being recorded, but the database only has them up to the last save.
/// To show them reaching now, the last state is stretched to now.
/// While the computer is active, the last window is stretched too.
/// </summary>
public static class OpenBlocks
{
    /// <summary>
    /// Stretches the last state, and the last window while the computer is active, to <paramref name="nowUtc"/>.
    /// Only blocks that ended no more than <paramref name="savedWithin"/> ago are stretched.
    /// Older ones mean recording has stopped.
    /// </summary>
    public static ActivityRecords UpTo(ActivityRecords records, DateTime nowUtc, TimeSpan savedWithin)
    {
        if (records.States.Count == 0)
        {
            return records;
        }

        var lastState = records.States[^1];
        if (!IsStillOpen(lastState.Range))
        {
            return records;
        }

        var states = records.States.SkipLast(1).Append(lastState with { Range = UpToNow(lastState.Range) }).ToList();
        var activities = records.Activities;
        if (lastState.State == ComputerState.Active && activities.Count > 0 && IsOpenInLastState(activities[^1].Range))
        {
            activities = [.. activities.SkipLast(1), activities[^1] with { Range = UpToNow(activities[^1].Range) }];
        }

        return records with { States = states, Activities = activities };

        bool IsStillOpen(TimeRange range) => range.End <= nowUtc && nowUtc - range.End <= savedWithin;

        // A window that ended before the last state began, such as one from before a lock, is no longer open.
        bool IsOpenInLastState(TimeRange range) => range.End >= lastState.Range.Start && IsStillOpen(range);

        TimeRange UpToNow(TimeRange range) => new(range.Start, nowUtc);
    }
}
