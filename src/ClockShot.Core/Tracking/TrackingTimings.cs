namespace ClockShot.Core.Tracking;

/// <summary>
/// Timings of the tracker that other parts of the app depend on.
/// </summary>
public static class TrackingTimings
{
    /// <summary>
    /// How often the end of the open segments is saved while they're still going, so a crash loses at most this much.
    /// The timeline uses it to tell a segment that is still being recorded from one that ended.
    /// </summary>
    public static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(15);
}
