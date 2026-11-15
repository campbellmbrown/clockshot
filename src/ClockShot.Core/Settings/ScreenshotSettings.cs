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

    /// <summary>
    /// Wider screenshots are scaled down to this width.
    /// </summary>
    public int MaxWidthPixels { get; init; } = 1920;

    /// <summary>
    /// WebP quality, from 10 for the smallest files to 100 for the best pictures.
    /// At 100 the picture is saved without loss.
    /// </summary>
    public int Quality { get; init; } = 70;

    /// <summary>
    /// Screenshots older than this many days are deleted.
    /// </summary>
    public int KeepForDays { get; init; } = 30;

    /// <summary>
    /// Process names whose windows are never captured.
    /// </summary>
    public IReadOnlyList<string> ExcludedProcessNames { get; init; } = [];
}
