using System.Runtime.InteropServices;

using ClockShot.Core.Screenshots;

using static ClockShot.Platform.Windows.Interop.NativeMethods;

namespace ClockShot.Platform.Windows;

/// <summary>
/// Copies the screen where a window is, including anything on top of it.
/// Copying from the screen works for apps that draw with the graphics card, such as browsers.
/// Asking the window to draw itself doesn't.
/// The process must be per-monitor DPI aware in app.manifest, so the screen uses the same physical pixels as the window's frame.
/// </summary>
public sealed class WindowsWindowCapturer : IWindowCapturer
{
    // Getting a drawing context for no window gives the whole screen.
    private const nint WholeScreen = 0;

    public CapturedImage? Capture(nint windowHandle)
    {
        if (windowHandle == 0 || IsIconic(windowHandle) || !IsWindowVisible(windowHandle) || IsCloaked(windowHandle))
        {
            return null;
        }

        if (FrameBounds(windowHandle) is not { } frame)
        {
            return null;
        }

        var bounds = ClipToScreens(frame, VirtualScreen());
        if (bounds.Right <= bounds.Left || bounds.Bottom <= bounds.Top)
        {
            return null;
        }

        return CopyFromScreen(bounds);
    }

    /// <summary>
    /// The part of the window inside the rectangle that holds every screen.
    /// The result has no width or height when the window is outside it.
    /// </summary>
    internal static Rect ClipToScreens(Rect window, Rect screens) => new()
    {
        Left = Math.Max(window.Left, screens.Left),
        Top = Math.Max(window.Top, screens.Top),
        Right = Math.Min(window.Right, screens.Right),
        Bottom = Math.Min(window.Bottom, screens.Bottom),
    };

    /// <summary>
    /// True when the window counts as visible but isn't shown, such as one on another virtual desktop.
    /// </summary>
    private static bool IsCloaked(nint window) =>
        DwmGetWindowAttribute(window, DwmwaCloaked, out int cloaked, sizeof(int)) == 0 && cloaked != 0;

    /// <summary>
    /// The window's visible frame in physical pixels, without the invisible border that's used for resizing.
    /// Returns null when Windows can't give it.
    /// </summary>
    private static Rect? FrameBounds(nint window) =>
        DwmGetWindowAttribute(window, DwmwaExtendedFrameBounds, out Rect bounds, Marshal.SizeOf<Rect>()) == 0
            ? bounds
            : null;

    /// <summary>
    /// The rectangle that holds every screen.
    /// </summary>
    private static Rect VirtualScreen()
    {
        var left = GetSystemMetrics(SmXVirtualScreen);
        var top = GetSystemMetrics(SmYVirtualScreen);
        return new Rect
        {
            Left = left,
            Top = top,
            Right = left + GetSystemMetrics(SmCxVirtualScreen),
            Bottom = top + GetSystemMetrics(SmCyVirtualScreen),
        };
    }

    private static CapturedImage? CopyFromScreen(Rect bounds)
    {
        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        nint screen = 0;
        nint memory = 0;
        nint bitmap = 0;
        nint previous = 0;
        try
        {
            screen = GetDC(WholeScreen);
            if (screen == 0)
            {
                return null;
            }

            memory = CreateCompatibleDC(screen);
            if (memory == 0)
            {
                return null;
            }

            var header = new BitmapInfoHeader
            {
                Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                Width = width,
                // A negative height puts the top row first.
                Height = -height,
                // Win32 requires 1.
                Planes = 1,
                BitCount = CapturedImage.BytesPerPixel * 8,
            };
            bitmap = CreateDIBSection(memory, ref header, DibRgbColors, out var bits, section: 0, offset: 0);
            if (bitmap == 0)
            {
                return null;
            }

            // Without the bitmap selected, the copy would land in a 1 x 1 placeholder and come out black.
            previous = SelectObject(memory, bitmap);
            if (previous == 0 || !BitBlt(memory, x: 0, y: 0, width, height, screen, bounds.Left, bounds.Top, SrcCopy))
            {
                return null;
            }

            var pixels = new byte[checked(width * height * CapturedImage.BytesPerPixel)];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            return new CapturedImage(width, height, pixels);
        }
        finally
        {
            Release(screen, memory, bitmap, previous);
        }
    }

    /// <summary>
    /// Frees whatever <see cref="CopyFromScreen"/> managed to create.
    /// A zero handle was never created.
    /// </summary>
    private static void Release(nint screen, nint memory, nint bitmap, nint previous)
    {
        if (previous != 0)
        {
            SelectObject(memory, previous);
        }

        if (bitmap != 0)
        {
            DeleteObject(bitmap);
        }

        if (memory != 0)
        {
            DeleteDC(memory);
        }

        if (screen != 0)
        {
            // Nothing useful can be done if releasing fails.
            _ = ReleaseDC(WholeScreen, screen);
        }
    }
}
