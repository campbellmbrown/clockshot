namespace ClockShot.Core.Settings;

public sealed record GeneralSettings
{
    /// <summary>
    /// Closing the main window hides it to the tray instead of exiting, so tracking continues.
    /// </summary>
    public bool CloseToTray { get; init; } = true;
}
