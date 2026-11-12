namespace ClockShot.App.Services;

/// <summary>
/// Opens things outside the app with the system's default program.
/// </summary>
public interface IShell
{
    /// <summary>
    /// Opens a folder in the system's file manager, creating it first if needed.
    /// </summary>
    void OpenFolder(string path);
}
