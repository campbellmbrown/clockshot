namespace ClockShot.Core.Settings;

public sealed record TrackingSettings
{
    /// <summary>
    /// No keyboard or mouse input for this many minutes counts as away.
    /// </summary>
    public int AwayAfterMinutes { get; init; } = 5;

    /// <summary>
    /// When coming back after being away, ask what you were doing.
    /// </summary>
    public bool AwayPromptEnabled { get; init; } = true;

    /// <summary>
    /// Only ask after being away at least this many minutes.
    /// </summary>
    public int AwayPromptAfterMinutes { get; init; } = 10;
}
