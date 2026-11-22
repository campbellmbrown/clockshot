namespace ClockShot.Core.Settings;

public sealed record GeneralSettings
{
    /// <summary>
    /// Closing the main window hides it to the tray instead of exiting, so tracking continues.
    /// </summary>
    public bool CloseToTray { get; init; } = true;

    /// <summary>
    /// When the app starts at sign-in, it shows just the tray icon instead of the window.
    /// </summary>
    public bool StartInTrayWithWindows { get; init; } = true;

    /// <summary>
    /// The local hour, from 0 to 23, at which a new day starts on the timeline.
    /// A later hour suits people who work past midnight.
    /// </summary>
    public int DayStartHour { get; init; }
}
