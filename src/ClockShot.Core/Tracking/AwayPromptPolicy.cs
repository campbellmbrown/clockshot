using ClockShot.Core.Models;
using ClockShot.Core.Settings;

namespace ClockShot.Core.Tracking;

/// <summary>
/// Decides whether to ask what the user was doing when they come back after being away.
/// </summary>
public static class AwayPromptPolicy
{
    /// <param name="isPromptOpen">True while an earlier prompt is still open, because only one is shown at a time.</param>
    public static bool ShouldAsk(TrackingSettings settings, TimeRange away, bool isPromptOpen) =>
        settings.AwayPromptEnabled
        && away.Duration >= TimeSpan.FromMinutes(settings.AwayPromptAfterMinutes)
        && !isPromptOpen;
}
