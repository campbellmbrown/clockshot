using ClockShot.Core.Tracking;

namespace ClockShot.App.Tests;

/// <summary>
/// Pauses and resumes at once, without a tracking loop.
/// </summary>
public sealed class FakeTrackingController(TrackingStatus status) : ITrackingController
{
    public event Action? StatusChanged;

    public TrackingStatus Status { get; private set; } = status;

    public void Pause() => SetStatus(TrackingStatus.Paused);

    public void Resume() => SetStatus(new TrackingStatus(IsPaused: false, State: null, Window: null));

    private void SetStatus(TrackingStatus status)
    {
        Status = status;
        StatusChanged?.Invoke();
    }
}
