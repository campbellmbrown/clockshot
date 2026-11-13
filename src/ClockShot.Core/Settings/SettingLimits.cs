namespace ClockShot.Core.Settings;

/// <summary>
/// The allowed range of each number setting.
/// The Settings screen and <see cref="AppSettings.Normalized"/> both use these.
/// </summary>
public static class SettingLimits
{
    // A longer limit than two hours would count most breaks as work.
    public static IntRange AwayAfterMinutes { get; } = new(1, 120);
}
