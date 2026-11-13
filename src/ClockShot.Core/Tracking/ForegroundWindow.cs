namespace ClockShot.Core.Tracking;

/// <summary>
/// The window the user is working in, as read from the OS.
/// </summary>
/// <param name="ProcessName">The executable's file name.</param>
/// <param name="ExecutablePath">The full path to the executable, when the OS allows reading it.</param>
/// <param name="DisplayName">The application's name as the user knows it.</param>
/// <param name="Title">The window's title.</param>
public sealed record ForegroundWindow(
    string ProcessName,
    string? ExecutablePath,
    string DisplayName,
    string Title)
{
    /// <summary>
    /// True when both windows belong to the same application and have the same title.
    /// </summary>
    public bool IsSameActivityAs(ForegroundWindow? other) =>
        other is not null && IsSameApplicationAs(other) && Title == other.Title;

    public bool IsSameApplicationAs(ForegroundWindow? other) =>
        other is not null
        && string.Equals(ProcessName, other.ProcessName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(ExecutablePath, other.ExecutablePath, StringComparison.OrdinalIgnoreCase);
}
