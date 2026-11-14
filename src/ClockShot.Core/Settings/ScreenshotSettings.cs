namespace ClockShot.Core.Settings;

public sealed record ScreenshotSettings
{
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Take a screenshot at least this often while active.
    /// </summary>
    public int IntervalSeconds { get; init; } = 60;
}
