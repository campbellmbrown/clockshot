namespace ClockShot.App.Controls;

/// <summary>
/// The name of every icon the app shows, by what it means.
/// Each value is a file name in Assets/Icons.
/// Use these instead of strings, so a misspelt name doesn't compile.
/// Several meanings can share a file.
/// </summary>
public static class Icons
{
    // ---- App ----
    public const string App = "clock";
    public const string Exit = "door-open";
    public const string About = "info";
    public const string OpenFolder = "folder-open";

    // ---- Pages ----
    public const string Settings = "settings";

    // ---- Tracking ----
    public const string PauseTracking = "circle-pause";
    public const string ResumeTracking = "circle-play";
}
