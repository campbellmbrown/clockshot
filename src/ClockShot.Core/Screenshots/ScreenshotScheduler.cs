using ClockShot.Core.Models;
using ClockShot.Core.Settings;
using ClockShot.Core.Tracking;

namespace ClockShot.Core.Screenshots;

/// <summary>
/// Decides when to take a screenshot.
/// It takes one every <see cref="ScreenshotSettings.IntervalSeconds"/> while active.
/// </summary>
public sealed class ScreenshotScheduler
{
    private DateTime? _lastCaptureUtc;

    /// <summary>
    /// Call once per sample.
    /// Returns true when a screenshot should be taken now.
    /// </summary>
    public bool ShouldCapture(TrackingSample sample, ComputerState? state, ScreenshotSettings settings)
    {
        var nowUtc = sample.TimeUtc;
        var window = sample.Window;

        if (!settings.Enabled || state != ComputerState.Active || window is null)
        {
            return false;
        }

        // A last capture in the future means the clock was moved back, so capture now instead of waiting for it to catch up.
        if (_lastCaptureUtc is not { } lastCapture || nowUtc < lastCapture)
        {
            return true;
        }

        var sinceLast = nowUtc - lastCapture;
        return sinceLast >= TimeSpan.FromSeconds(settings.IntervalSeconds);
    }

    /// <summary>
    /// Call after a screenshot was taken.
    /// </summary>
    public void MarkCaptured(DateTime nowUtc) => _lastCaptureUtc = nowUtc;
}
