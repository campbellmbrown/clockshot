using System.Diagnostics;

namespace ClockShot.App.Services;

/// <summary>
/// Lets the operating system pick the program.
/// This works on Windows, macOS and Linux.
/// </summary>
internal sealed class SystemShell : IShell
{
    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        using var _ = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
