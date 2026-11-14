using ClockShot.Core.Models;

namespace ClockShot.Core.Tracking;

/// <summary>
/// What the tracker is doing right now.
/// </summary>
/// <param name="IsPaused">True when the user paused tracking.</param>
/// <param name="State">Active, away or locked. Null while paused or before tracking starts.</param>
/// <param name="Window">The window being recorded, or null.</param>
public sealed record TrackingStatus(bool IsPaused, ComputerState? State, ForegroundWindow? Window)
{
    public static TrackingStatus NotStarted { get; } = new(IsPaused: false, State: null, Window: null);

    public static TrackingStatus Paused { get; } = new(IsPaused: true, State: null, Window: null);
}
