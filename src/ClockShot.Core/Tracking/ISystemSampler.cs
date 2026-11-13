namespace ClockShot.Core.Tracking;

/// <summary>
/// Reads the foreground window, idle time and lock state from the OS.
/// </summary>
public interface ISystemSampler
{
    TrackingSample Read(DateTime timeUtc);
}
