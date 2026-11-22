using System.Collections.Concurrent;
using System.Diagnostics;

using ClockShot.Core.Tracking;

using static ClockShot.Platform.Windows.Interop.NativeMethods;

namespace ClockShot.Platform.Windows;

/// <summary>
/// Reads the foreground window, idle time and lock state from Windows.
/// </summary>
public sealed unsafe class WindowsSystemSampler : ISystemSampler
{
    // Longer titles are cut, which keeps the buffer on the stack small.
    private const int MaxTitleLength = 512;

    // Desktop names are short words, so this leaves plenty of room.
    private const int MaxDesktopNameLength = 64;

    // The desktop that holds the user's windows while they're signed in and the screen isn't locked.
    private const string NormalDesktopName = "Default";

    private readonly ConcurrentDictionary<string, string> _displayNames = new(StringComparer.OrdinalIgnoreCase);

    public TrackingSample Read(DateTime timeUtc) =>
        new(timeUtc, ReadForegroundWindow(), ReadIdleTime(), IsSessionLocked());

    /// <summary>
    /// Turns the buffer Windows filled into the desktop name.
    /// </summary>
    /// <param name="buffer">The characters Windows wrote, ending with a null character.</param>
    /// <param name="bytesWritten">How many bytes Windows wrote, including the null character.</param>
    internal static string DesktopName(ReadOnlySpan<char> buffer, int bytesWritten)
    {
        var length = Math.Min(buffer.Length, bytesWritten / sizeof(char));
        return buffer[..length].ToString().TrimEnd('\0');
    }

    /// <summary>
    /// Any desktop other than the normal one means the session is locked, such as the lock screen's "Winlogon".
    /// </summary>
    internal static bool IsLockScreen(string desktopName) =>
        !desktopName.Equals(NormalDesktopName, StringComparison.OrdinalIgnoreCase);

    private ForegroundWindow? ReadForegroundWindow()
    {
        var window = GetForegroundWindow();
        if (window == 0)
        {
            return null;
        }

        var title = WindowTitle(window);
        var executablePath = WindowOwner.ExecutablePath(window);
        var processName = WindowOwner.ProcessName(executablePath);
        var displayName = DisplayName(executablePath, processName);
        return new ForegroundWindow(window, processName, executablePath, displayName, title);
    }

    private static string WindowTitle(nint window)
    {
        var length = Math.Min(GetWindowTextLength(window), MaxTitleLength);
        if (length <= 0)
        {
            return "";
        }

        var buffer = stackalloc char[length + 1];
        var copied = GetWindowText(window, buffer, length + 1);
        return new string(buffer, 0, copied);
    }

    /// <summary>
    /// The executable's description.
    /// Falls back to its product name, then its file name.
    /// </summary>
    private string DisplayName(string? executablePath, string processName)
    {
        if (executablePath is null)
        {
            return Path.GetFileNameWithoutExtension(processName);
        }

        return _displayNames.GetOrAdd(executablePath, path =>
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(path);
                return FirstNonBlank(info.FileDescription, info.ProductName) ?? Path.GetFileNameWithoutExtension(path);
            }
            catch (IOException)
            {
                return Path.GetFileNameWithoutExtension(path);
            }
        });
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private static TimeSpan ReadIdleTime()
    {
        var info = new LastInputInfo { Size = (uint)sizeof(LastInputInfo) };
        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        // Both are 32-bit counts of milliseconds since boot. Unchecked subtraction handles the wrap every 49 days.
        var idleMilliseconds = unchecked((uint)Environment.TickCount - info.Time);
        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }

    /// <summary>
    /// Reads the name of the desktop that gets keyboard and mouse input.
    /// Windows refuses to open that desktop while the session is locked, which also counts as locked.
    /// </summary>
    private static bool IsSessionLocked()
    {
        const uint DesktopReadObjects = 0x0001;
        var desktop = OpenInputDesktop(flags: 0, inherit: false, desiredAccess: DesktopReadObjects);
        if (desktop == 0)
        {
            return true;
        }

        try
        {
            var buffer = stackalloc char[MaxDesktopNameLength];
            var bufferBytes = MaxDesktopNameLength * sizeof(char);
            if (!GetUserObjectInformation(desktop, UoiName, buffer, bufferBytes, out var bytesWritten))
            {
                // Without a name there's no sign of a lock, so tracking carries on as normal.
                return false;
            }

            return IsLockScreen(DesktopName(new ReadOnlySpan<char>(buffer, MaxDesktopNameLength), bytesWritten));
        }
        finally
        {
            CloseDesktop(desktop);
        }
    }
}
