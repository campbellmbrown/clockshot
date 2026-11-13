namespace ClockShot.Core.Tracking;

/// <summary>
/// Records tracking changes.
/// The sampling loop calls it, so it must not keep the loop waiting.
/// </summary>
public interface ITrackingSink
{
    /// <summary>
    /// Starts recording in the background. Changes written before this wait until it's called.
    /// </summary>
    void Start();

    void Write(IReadOnlyList<TrackingChange> changes);

    /// <summary>
    /// Saves everything still queued and stops accepting changes. Call once, before exit.
    /// </summary>
    Task CompleteAsync();
}
