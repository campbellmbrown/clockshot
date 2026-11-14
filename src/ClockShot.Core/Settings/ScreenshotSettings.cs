namespace ClockShot.Core.Settings;

public sealed record ScreenshotSettings
{
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Take a screenshot at least this often while active.
    /// </summary>
    public int IntervalSeconds { get; init; } = 60;

    /// <summary>
    /// Also take one shortly after switching to a different application.
    /// </summary>
    public bool CaptureOnAppSwitch { get; init; } = true;

    /// <summary>
    /// Never take screenshots closer together than this.
    /// </summary>
    public int MinimumGapSeconds { get; init; } = 10;
}
