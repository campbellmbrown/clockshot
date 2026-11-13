namespace ClockShot.Core.Tracking;

/// <summary>
/// One reading of what the computer is doing.
/// </summary>
/// <param name="TimeUtc">When the reading was taken.</param>
/// <param name="Window">The foreground window, or null when no window has focus.</param>
/// <param name="IdleTime">The time since the last keyboard or mouse input.</param>
public sealed record TrackingSample(DateTime TimeUtc, ForegroundWindow? Window, TimeSpan IdleTime);
