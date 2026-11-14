namespace ClockShot.Core.Tracking;

/// <summary>
/// Lets the UI see and control tracking.
/// </summary>
public interface ITrackingController
{
    /// <summary>
    /// Raised when <see cref="Status"/> changes.
    /// May be raised on any thread.
    /// </summary>
    event Action? StatusChanged;

    TrackingStatus Status { get; }

    /// <summary>
    /// Stops recording until <see cref="Resume"/>.
    /// The open segments end at the next sample, so paused time has none.
    /// </summary>
    void Pause();

    /// <summary>
    /// Starts recording again from the next sample.
    /// </summary>
    void Resume();
}
