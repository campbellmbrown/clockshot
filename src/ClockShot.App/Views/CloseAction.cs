namespace ClockShot.App.Views;

/// <summary>
/// What the main window does when something asks it to close.
/// </summary>
public enum CloseAction
{
    /// <summary>
    /// The window closes, because the app is already ending or code asked for the close.
    /// </summary>
    Close,

    HideToTray,

    ExitApp,
}
