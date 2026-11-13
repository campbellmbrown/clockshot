namespace ClockShot.Core.Settings;

public sealed record TrackingSettings
{
    /// <summary>
    /// No keyboard or mouse input for this many minutes counts as away.
    /// </summary>
    public int AwayAfterMinutes { get; init; } = 5;
}
