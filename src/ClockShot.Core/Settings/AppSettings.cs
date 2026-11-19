namespace ClockShot.Core.Settings;

/// <summary>
/// All user settings.
/// They can't be changed in place.
/// Change them with <c>with</c> expressions and save them through <see cref="ISettingsStore"/>.
/// Each number's unit is part of its name, so the saved JSON explains itself.
/// </summary>
public sealed record AppSettings
{
    public GeneralSettings General { get; init; } = new();
    public TrackingSettings Tracking { get; init; } = new();
    public ScreenshotSettings Screenshots { get; init; } = new();
    public LayoutSettings Layout { get; init; } = new();

    /// <summary>
    /// A copy with every value inside its <see cref="SettingLimits"/> range and missing parts set to defaults.
    /// A hand-edited or damaged settings file then can't crash the app.
    /// </summary>
    public AppSettings Normalized()
    {
        var general = General ?? new();
        var tracking = Tracking ?? new();
        var screenshots = Screenshots ?? new();
        var screenshotInterval = SettingLimits.ScreenshotIntervalSeconds.Clamp(screenshots.IntervalSeconds);
        var layout = Layout ?? new();
        return this with
        {
            General = general with { DayStartHour = SettingLimits.DayStartHour.Clamp(general.DayStartHour) },
            Tracking = tracking with
            {
                AwayAfterMinutes = SettingLimits.AwayAfterMinutes.Clamp(tracking.AwayAfterMinutes),
            },
            Screenshots = screenshots with
            {
                IntervalSeconds = screenshotInterval,

                // A gap longer than the interval would silently replace it.
                MinimumGapSeconds = Math.Min(
                    SettingLimits.ScreenshotMinimumGapSeconds.Clamp(screenshots.MinimumGapSeconds),
                    screenshotInterval),

                MaxWidthPixels = SettingLimits.ScreenshotMaxWidthPixels.Clamp(screenshots.MaxWidthPixels),
                Quality = SettingLimits.ScreenshotQuality.Clamp(screenshots.Quality),
                KeepForDays = SettingLimits.KeepScreenshotsForDays.Clamp(screenshots.KeepForDays),
                ExcludedProcessNames = Cleaned(screenshots.ExcludedProcessNames),
                PreviewMode = Enum.IsDefined(screenshots.PreviewMode) ? screenshots.PreviewMode : new ScreenshotSettings().PreviewMode,
            },
            Layout = layout with
            {
                // Only a hand-edited file can hold a negative size, which a grid can't lay out.
                TimelineHeight = Math.Max(0, layout.TimelineHeight),
                DetailsPanelWidth = Math.Max(0, layout.DetailsPanelWidth),
            },
        };
    }

    /// <summary>
    /// Trims the names and drops blank ones, which a hand-edited settings file can have.
    /// A list that's already clean is returned as it is, so settings that are already normal stay equal.
    /// </summary>
    private static IReadOnlyList<string> Cleaned(IReadOnlyList<string>? names)
    {
        if (names is null)
        {
            return [];
        }

        return names.All(name => !string.IsNullOrWhiteSpace(name) && name == name.Trim())
            ? names
            : [.. names.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim())];
    }
}
