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

    /// <summary>
    /// A copy with every value inside its <see cref="SettingLimits"/> range and missing parts set to defaults.
    /// A hand-edited or damaged settings file then can't crash the app.
    /// </summary>
    public AppSettings Normalized()
    {
        var general = General ?? new();
        var tracking = Tracking ?? new();
        return this with
        {
            General = general,
            Tracking = tracking with
            {
                AwayAfterMinutes = SettingLimits.AwayAfterMinutes.Clamp(tracking.AwayAfterMinutes),
            },
        };
    }
}
