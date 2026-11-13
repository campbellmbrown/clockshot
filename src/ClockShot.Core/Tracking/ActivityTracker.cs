using ClockShot.Core.Models;

namespace ClockShot.Core.Tracking;

/// <summary>
/// Turns samples of what the computer is doing into segments.
/// A segment is a stretch of time with a start and an end.
/// An activity segment says which application and window was in use.
/// A computer-state segment says whether the computer was active, away or locked.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Activity is only recorded while the computer is <see cref="ComputerState.Active"/>.</item>
/// <item>Going away is dated from the last input, not from when the away limit was reached.
/// The activity before it is trimmed back to the last input.</item>
/// <item>A gap between samples longer than <c>maxSampleGap</c> ends everything at the last sample.
/// Sleep, hibernation or a hung process cause such gaps, and that time counts as not running.</item>
/// </list>
/// </remarks>
/// <param name="maxSampleGap">
/// A longer gap between samples means the computer was asleep or ClockShot wasn't sampling.
/// </param>
public sealed class ActivityTracker(TimeSpan maxSampleGap)
{
    private DateTime? _lastSampleUtc;
    private DateTime _activityStartUtc;
    private DateTime _stateStartUtc;

    /// <summary>
    /// The window of the open activity segment, or null when none is open.
    /// </summary>
    public ForegroundWindow? CurrentWindow { get; private set; }

    /// <summary>
    /// The state of the open computer-state segment, or null when none is open.
    /// </summary>
    public ComputerState? CurrentState { get; private set; }

    /// <summary>
    /// Works out what changed since the last sample.
    /// </summary>
    /// <param name="awayAfter">No input for this long means the user is away.</param>
    public IReadOnlyList<TrackingChange> Process(TrackingSample sample, TimeSpan awayAfter)
    {
        var changes = new List<TrackingChange>();

        if (_lastSampleUtc is { } last && sample.TimeUtc - last > maxSampleGap)
        {
            EndAll(last, changes);
        }

        var state = StateOf(sample, awayAfter);
        if (state != CurrentState)
        {
            ChangeState(state, StateChangeTime(sample, state), changes);
        }

        if (state == ComputerState.Active && sample.Window?.IsSameActivityAs(CurrentWindow) != true)
        {
            EndActivity(sample.TimeUtc, changes);
            if (sample.Window is { } window)
            {
                StartActivity(window, sample.TimeUtc, changes);
            }
        }

        changes.Add(new SegmentsExtended(sample.TimeUtc));
        _lastSampleUtc = sample.TimeUtc;
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

    private static ComputerState StateOf(TrackingSample sample, TimeSpan awayAfter) => sample switch
    {
        { IsLocked: true } => ComputerState.Locked,
        _ when sample.IdleTime >= awayAfter => ComputerState.Away,
        _ => ComputerState.Active,
    };

    /// <summary>
    /// Away starts at the last input. Coming back from away also dates from the last input, when the user
    /// touched the mouse or keyboard again. Locking and unlocking are dated at the sample.
    /// </summary>
    private DateTime StateChangeTime(TrackingSample sample, ComputerState newState)
    {
        var isLockChange = newState == ComputerState.Locked || CurrentState == ComputerState.Locked;
        var isAwayChange = newState == ComputerState.Away || CurrentState == ComputerState.Away;
        var time = isAwayChange && !isLockChange ? sample.TimeUtc - sample.IdleTime : sample.TimeUtc;
        return TimeMath.Max(time, TimeMath.Max(_stateStartUtc, _activityStartUtc));
    }

    private void ChangeState(ComputerState state, DateTime timeUtc, List<TrackingChange> changes)
    {
        if (state != ComputerState.Active)
        {
            EndActivity(timeUtc, changes);
        }

        if (CurrentState is not null)
        {
            changes.Add(new StateEnded(timeUtc));
        }

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
