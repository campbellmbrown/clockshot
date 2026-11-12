namespace ClockShot.Core;

/// <summary>
/// The app's name, kept in one place.
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// The name people see. The data folder, mutex, pipe and Run key value use it too.
    /// </summary>
    public const string Name = "ClockShot";

    /// <summary>
    /// The lower-case name that starts file names, like the database and log files.
    /// </summary>
    public static readonly string FilePrefix = Name.ToLowerInvariant();
}
