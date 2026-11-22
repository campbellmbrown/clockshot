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

    [LibraryImport("user32.dll")]
    public static partial nint GetWindow(nint window, uint relation);

    // Asks GetWindow for the window just in front.
    public const uint GwHwndPrev = 3;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(nint window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindowVisible(nint window);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmGetWindowAttribute(nint window, int attribute, out Rect value, int size);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);

    public const int DwmwaExtendedFrameBounds = 9;
    public const int DwmwaCloaked = 14;

    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetrics(int index);

    public const int SmXVirtualScreen = 76;
    public const int SmYVirtualScreen = 77;
    public const int SmCxVirtualScreen = 78;
    public const int SmCyVirtualScreen = 79;

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

    // ---- GDI ----

    [LibraryImport("user32.dll")]
    public static partial nint GetDC(nint window);

    [LibraryImport("user32.dll")]
    public static partial int ReleaseDC(nint window, nint deviceContext);

    [LibraryImport("gdi32.dll")]
    public static partial nint CreateCompatibleDC(nint deviceContext);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(nint deviceContext);

    [LibraryImport("gdi32.dll")]
    public static partial nint CreateDIBSection(
        nint deviceContext,
        ref BitmapInfoHeader info,
        uint usage,
        out nint bits,
        nint section,
        uint offset);

    [LibraryImport("gdi32.dll")]
    public static partial nint SelectObject(nint deviceContext, nint gdiObject);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint gdiObject);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool BitBlt(
        nint destination,
        int x,
        int y,
        int width,
        int height,
        nint source,
        int sourceX,
        int sourceY,
        uint rasterOperation);

    public const uint SrcCopy = 0x00CC0020;

    // Asks CreateDIBSection for pixels with their own colours, not indexes into a palette.
    public const uint DibRgbColors = 0;

    // ---- Structs ----

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    /// <summary>
    /// CreateDIBSection takes a BITMAPINFO.
    /// It reads only this header for a 32-bit image with no colour table.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }
}
