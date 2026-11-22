using System.Runtime.InteropServices;

namespace ClockShot.Platform.Windows.Tests;

/// <summary>
/// A plain window of the test process, so tests can arrange windows on the real desktop.
/// Dispose it on the thread that created it.
/// </summary>
internal sealed partial class TestWindow : IDisposable
{
    // A window class every Windows desktop has, so the tests don't register their own.
    private const string StaticClass = "STATIC";

    // Window styles.
    private const uint Popup = 0x80000000;
    private const uint Visible = 0x10000000;

    // Extended window styles. A tool window stays off the taskbar.
    private const uint ToolWindow = 0x00000080;
    private const uint Topmost = 0x00000008;
    private const uint NoActivate = 0x08000000;

    private TestWindow(nint handle) => Handle = handle;

    public nint Handle { get; }

    /// <summary>
    /// A shown window in front of every window that isn't always on top.
    /// </summary>
    public static TestWindow InFront(int x, int y, int size) => Create(x, y, size, Visible, Topmost);

    /// <summary>
    /// A shown window behind every window that's always on top.
    /// </summary>
    public static TestWindow Behind(int x, int y, int size) => Create(x, y, size, Visible, extendedStyle: 0);

    /// <summary>
    /// A hidden window in front of every window that isn't always on top.
    /// </summary>
    public static TestWindow HiddenInFront(int x, int y, int size) => Create(x, y, size, style: 0, Topmost);

    public void Dispose() => DestroyWindow(Handle);

    private static TestWindow Create(int x, int y, int size, uint style, uint extendedStyle)
    {
        var handle = CreateWindowEx(
            ToolWindow | NoActivate | extendedStyle,
            StaticClass,
            windowName: "",
            Popup | style,
            x,
            y,
            width: size,
            height: size,
            parent: 0,
            menu: 0,
            instance: 0,
            parameter: 0);
        return handle == 0
            ? throw new InvalidOperationException($"Creating a test window failed with error {Marshal.GetLastPInvokeError()}.")
            : new TestWindow(handle);
    }

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateWindowEx(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyWindow(nint window);
}
