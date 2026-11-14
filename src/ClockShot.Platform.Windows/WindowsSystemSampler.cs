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

    // Longer executable paths are rare. Their windows show as UnknownProcessName.
    private const int MaxPathLength = 1024;

    // Desktop names are short words, so this leaves plenty of room.
    private const int MaxDesktopNameLength = 64;

    // Used when Windows won't say which program owns the window. Protected system processes hide it.
    private const string UnknownProcessName = "Unknown";

    // Store apps run inside a frame from this program. The real app owns a child window of the frame.
    private const string StoreAppHost = "ApplicationFrameHost.exe";

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
        var executablePath = ExecutablePath(ProcessIdOf(window));
        if (executablePath is not null && IsStoreAppHost(executablePath))
        {
            executablePath = StoreAppExecutablePath(window) ?? executablePath;
        }

        var processName = executablePath is null ? UnknownProcessName : Path.GetFileName(executablePath);
        var displayName = DisplayName(executablePath, processName);
        return new ForegroundWindow(window, processName, executablePath, displayName, title);
    }

    private static bool IsStoreAppHost(string executablePath) =>
        Path.GetFileName(executablePath).Equals(StoreAppHost, StringComparison.OrdinalIgnoreCase);

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

    private static uint ProcessIdOf(nint window)
    {
        GetWindowThreadProcessId(window, out var processId);
        return processId;
    }

    private static string? ExecutablePath(uint processId)
    {
        var process = OpenProcess(ProcessQueryLimitedInformation, inheritHandle: false, processId);
        if (process == 0)
        {
            return null;
        }

        try
        {
            var buffer = stackalloc char[MaxPathLength];
            var size = MaxPathLength;
            return QueryFullProcessImageName(process, ProcessNameWin32, buffer, ref size)
                ? new string(buffer, 0, size)
                : null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    /// <summary>
    /// Finds the executable of the Store app inside a frame window, which owns a child window of the frame.
    /// </summary>
    private static string? StoreAppExecutablePath(nint frame)
    {
        var frameProcess = ProcessIdOf(frame);
        foreach (var child in ChildWindows(frame))
        {
            var childProcess = ProcessIdOf(child);
            if (childProcess != frameProcess)
            {
                return ExecutablePath(childProcess);
            }
        }

        return null;
    }

    private static IEnumerable<nint> ChildWindows(nint parent)
    {
        // Null names match any child. Starting after window 0 starts at the first child.
        var child = FindWindowEx(parent, childAfter: 0, className: null, windowName: null);
        while (child != 0)
        {
            yield return child;
            child = FindWindowEx(parent, childAfter: child, className: null, windowName: null);
        }
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
