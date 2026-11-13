using System.Runtime.InteropServices;

namespace ClockShot.Platform.Windows.Interop;

/// <summary>
/// The Win32 functions ClockShot uses. Kept in one place so the rest of the code reads as plain C#.
/// </summary>
internal static unsafe partial class NativeMethods
{
    // ---- Windows ----

    [LibraryImport("user32.dll")]
    public static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    public static partial uint GetWindowThreadProcessId(nint window, out uint processId);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
    public static partial int GetWindowTextLength(nint window);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW")]
    public static partial int GetWindowText(nint window, char* buffer, int maxCount);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint FindWindowEx(nint parent, nint childAfter, string? className, string? windowName);

    // ---- Input and session ----

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetLastInputInfo(ref LastInputInfo info);

    [LibraryImport("user32.dll")]
    public static partial nint OpenInputDesktop(
        uint flags,
        [MarshalAs(UnmanagedType.Bool)] bool inherit,
        uint desiredAccess);

    [LibraryImport("user32.dll", EntryPoint = "GetUserObjectInformationW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetUserObjectInformation(
        nint handle,
        int index,
        char* buffer,
        int length,
        out int lengthNeeded);

    public const int UoiName = 2;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseDesktop(nint desktop);

    // ---- Processes ----

    [LibraryImport("kernel32.dll")]
    public static partial nint OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId);

    public const uint ProcessQueryLimitedInformation = 0x1000;

    // Asks QueryFullProcessImageName for a normal path, not a device path.
    public const uint ProcessNameWin32 = 0;

    [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool QueryFullProcessImageName(nint process, uint flags, char* buffer, ref int size);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    // ---- Structs ----

    [StructLayout(LayoutKind.Sequential)]
    public struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }
}
