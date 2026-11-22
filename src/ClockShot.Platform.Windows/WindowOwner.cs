using static ClockShot.Platform.Windows.Interop.NativeMethods;

namespace ClockShot.Platform.Windows;

/// <summary>
/// Finds the program that owns a window.
/// </summary>
internal static unsafe class WindowOwner
{
    // Longer executable paths are rare. Their windows show as UnknownProcessName.
    private const int MaxPathLength = 1024;

    // Used when Windows won't say which program owns the window. Protected system processes hide it.
    private const string UnknownProcessName = "Unknown";

    // Store apps run inside a frame from this program. The real app owns a child window of the frame.
    private const string StoreAppHost = "ApplicationFrameHost.exe";

    /// <summary>
    /// The full path of the window's executable, or null when Windows won't give it.
    /// For a Store app it's the app inside the frame, not the frame.
    /// </summary>
    public static string? ExecutablePath(nint window)
    {
        var executablePath = ExecutablePath(ProcessIdOf(window));
        if (executablePath is not null && IsStoreAppHost(executablePath))
        {
            executablePath = StoreAppExecutablePath(window) ?? executablePath;
        }

        return executablePath;
    }

    /// <summary>
    /// The executable's file name, or <see cref="UnknownProcessName"/> for no path.
    /// </summary>
    public static string ProcessName(string? executablePath) =>
        executablePath is null ? UnknownProcessName : Path.GetFileName(executablePath);

    private static bool IsStoreAppHost(string executablePath) =>
        Path.GetFileName(executablePath).Equals(StoreAppHost, StringComparison.OrdinalIgnoreCase);

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
}
