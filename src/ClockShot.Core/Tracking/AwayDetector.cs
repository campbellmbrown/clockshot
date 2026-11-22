using ClockShot.Core.Models;

namespace ClockShot.Core.Tracking;

/// <summary>
/// Notices when the user comes back to the computer after being away.
/// Being idle, locked or asleep all count as away, and sleep shows up as a gap in tracking.
/// Pausing tracking on purpose isn't being away, so call <see cref="Reset"/> on resume.
/// </summary>
public sealed class AwayDetector
{
    private DateTime? _awaySince;

    /// <summary>
    /// Takes the changes from one sample of the <see cref="ActivityTracker"/>.
    /// </summary>
    /// <returns>The time away that just ended, or null if the user didn't just come back.</returns>
    public TimeRange? Observe(IReadOnlyList<TrackingChange> changes)
    {
        TimeRange? returned = null;
        DateTime? lastEnd = null;
        foreach (var change in changes)
        {
            switch (change)
            {
                case StateEnded ended:
                    lastEnd = ended.EndUtc;
                    break;
                case StateStarted started:
                    returned = ObserveStart(started, lastEnd) ?? returned;
                    lastEnd = null;
                    break;
            }
        }

        // Tracking stopped with nothing after it yet, so the user has been away since it stopped.
        if (lastEnd is { } stoppedAt)
        {
            _awaySince ??= stoppedAt;
        }

        return returned;
    }

    /// <summary>
    /// Forgets any away time in progress, e.g. after the user paused tracking themselves.
    /// </summary>
    public void Reset() => _awaySince = null;

    /// <summary>
    /// Updates the away time for a state that just started.
    /// </summary>
    /// <param name="lastEnd">When the previous state ended in this batch, or null if none did.</param>
    /// <returns>The time away, when <paramref name="started"/> ends it.</returns>
    private TimeRange? ObserveStart(StateStarted started, DateTime? lastEnd)
    {
        // A state that starts later than the last one ended means tracking stopped in between.
        // The computer was asleep or samples stopped for a while, and that counts as away too.
        if (lastEnd is { } end && started.StartUtc > end)
        {
            _awaySince ??= end;
        }

        if (started.State is ComputerState.Away or ComputerState.Locked)
        {
            _awaySince ??= started.StartUtc;
            return null;
        }

        if (_awaySince is not { } since)
        {
            return null;
        }

        _awaySince = null;
        return started.StartUtc > since ? new TimeRange(since, started.StartUtc) : null;
    }
}
