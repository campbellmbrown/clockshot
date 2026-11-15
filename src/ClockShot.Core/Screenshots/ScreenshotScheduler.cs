using ClockShot.Core.Models;
using ClockShot.Core.Settings;
using ClockShot.Core.Tracking;

namespace ClockShot.Core.Screenshots;

/// <summary>
/// Decides when to take a screenshot.
/// It takes one every <see cref="ScreenshotSettings.IntervalSeconds"/> while active, and shortly after switching application.
/// Screenshots are never closer together than <see cref="ScreenshotSettings.MinimumGapSeconds"/>.
/// </summary>
public sealed class ScreenshotScheduler
{
    /// <summary>
    /// Wait this long after an application switch, so the new window has finished drawing.
    /// </summary>
    private static readonly TimeSpan SwitchSettleDelay = TimeSpan.FromSeconds(2);

    private DateTime? _lastCaptureUtc;
    private DateTime? _appSwitchUtc;
    private ForegroundWindow? _lastWindow;

    /// <summary>
    /// Call once per sample.
    /// Returns true when a screenshot should be taken now.
    /// </summary>
    public bool ShouldCapture(TrackingSample sample, ComputerState? state, ScreenshotSettings settings)
    {
        var nowUtc = sample.TimeUtc;
        var window = sample.Window;

        if (window is not null && !window.IsSameApplicationAs(_lastWindow))
        {
            _appSwitchUtc = nowUtc;
        }

        _lastWindow = window;

        if (!settings.Enabled || state != ComputerState.Active || window is null || IsExcluded(window, settings))
        {
            return false;
        }

        // A last capture in the future means the clock was moved back, so capture now instead of waiting for it to catch up.
        if (_lastCaptureUtc is not { } lastCapture || nowUtc < lastCapture)
        {
            return true;
        }

        var sinceLast = nowUtc - lastCapture;
        if (sinceLast < TimeSpan.FromSeconds(settings.MinimumGapSeconds))
        {
            return false;
        }

        if (sinceLast >= TimeSpan.FromSeconds(settings.IntervalSeconds))
        {
            return true;
        }

        return settings.CaptureOnAppSwitch
            && _appSwitchUtc is { } switchedAt
            && nowUtc - switchedAt >= SwitchSettleDelay;
    }

    /// <summary>
    /// Call after a screenshot was taken.
    /// </summary>
    public void MarkCaptured(DateTime nowUtc)
    {
        _lastCaptureUtc = nowUtc;

        // Any app switch so far is in this screenshot, so it needs no capture of its own.
        _appSwitchUtc = null;
    }

    /// <summary>
    /// True when the window's process is excluded, by its name with or without the extension.
    /// </summary>
    private static bool IsExcluded(ForegroundWindow window, ScreenshotSettings settings)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(window.ProcessName);
        return settings.ExcludedProcessNames.Any(excluded =>
            string.Equals(excluded, window.ProcessName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(excluded, nameWithoutExtension, StringComparison.OrdinalIgnoreCase));
    }
}
