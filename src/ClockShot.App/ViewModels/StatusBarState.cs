namespace ClockShot.App.ViewModels;

/// <summary>
/// What the status bar shows about tracking.
/// </summary>
public enum StatusBarState
{
    /// <summary>
    /// Tracking hasn't taken its first sample yet.
    /// </summary>
    Starting,

    Tracking,

    Paused,

    Away,

    Locked,
}
