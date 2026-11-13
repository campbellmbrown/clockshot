using ClockShot.Core.Models;

namespace ClockShot.Core.Tracking;

/// <summary>
/// Turns samples of what the computer is doing into segments.
/// A segment is a stretch of time with a start and an end.
/// An activity segment says which application and window was in use.
/// A computer-state segment says when the computer was active.
/// </summary>
public sealed class ActivityTracker
{
    private DateTime _activityStartUtc;
    private DateTime _stateStartUtc;

    /// <summary>
    /// The state of the open computer-state segment, or null when none is open.
    /// </summary>
    public ComputerState? CurrentState { get; private set; }

    /// <summary>
    /// The window of the open activity segment, or null when none is open.
    /// </summary>
    private ForegroundWindow? CurrentWindow { get; set; }

    public IReadOnlyList<TrackingChange> Process(TrackingSample sample)
    {
        var changes = new List<TrackingChange>();

        if (CurrentState is null)
        {
            ChangeState(ComputerState.Active, sample.TimeUtc, changes);
        }

        if (sample.Window?.IsSameActivityAs(CurrentWindow) != true)
        {
            EndActivity(sample.TimeUtc, changes);
            if (sample.Window is { } window)
            {
                StartActivity(window, sample.TimeUtc, changes);
            }
        }

        changes.Add(new SegmentsExtended(sample.TimeUtc));
        return changes;
    }

    /// <summary>
    /// Ends all open segments.
    /// </summary>
    public IReadOnlyList<TrackingChange> Stop(DateTime timeUtc)
    {
        var changes = new List<TrackingChange>();
        EndAll(timeUtc, changes);
        return changes;
    }

    private void ChangeState(ComputerState state, DateTime timeUtc, List<TrackingChange> changes)
    {
        CurrentState = state;
        _stateStartUtc = timeUtc;
        changes.Add(new StateStarted(timeUtc, state));
    }

    private void StartActivity(ForegroundWindow window, DateTime timeUtc, List<TrackingChange> changes)
    {
        CurrentWindow = window;
        _activityStartUtc = timeUtc;
        changes.Add(new ActivityStarted(timeUtc, window));
    }

    private void EndActivity(DateTime timeUtc, List<TrackingChange> changes)
    {
        if (CurrentWindow is null)
        {
            return;
        }

        CurrentWindow = null;
        changes.Add(new ActivityEnded(TimeMath.Max(timeUtc, _activityStartUtc)));
    }

    private void EndAll(DateTime timeUtc, List<TrackingChange> changes)
    {
        EndActivity(timeUtc, changes);
        if (CurrentState is null)
        {
            return;
        }

        CurrentState = null;
        changes.Add(new StateEnded(TimeMath.Max(timeUtc, _stateStartUtc)));
    }
}
