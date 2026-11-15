namespace ClockShot.Core.Settings;

/// <summary>
/// The allowed range of each number setting.
/// The Settings screen and <see cref="AppSettings.Normalized"/> both use these.
/// </summary>
public static class SettingLimits
{
    // A longer limit than two hours would count most breaks as work.
    public static IntRange AwayAfterMinutes { get; } = new(1, 120);

    // Faster than every 10 seconds fills the disk. Slower than hourly leaves hours without a picture.
    public static IntRange ScreenshotIntervalSeconds { get; } = new(10, 3600);

    // The tracker samples once a second, so a shorter gap would mean nothing.
    // The gap is capped at the interval, so it never needs to be longer than the longest interval.
    public static IntRange ScreenshotMinimumGapSeconds { get; } = new(1, 3600);

    // Below 640 pixels wide, the text in a screenshot can't be read.
    // A 4K screen is 3840 pixels wide, so a wider limit would never be reached.
    public static IntRange ScreenshotMaxWidthPixels { get; } = new(640, 3840);

    // WebP quality goes up to 100. Below 10 the text in a screenshot can't be read.
    public static IntRange ScreenshotQuality { get; } = new(10, 100);

    // At least a day, so the cleanup never deletes a screenshot file that's still being written.
    // Ten years is longer than anyone keeps them.
    public static IntRange KeepScreenshotsForDays { get; } = new(1, 3650);
}
