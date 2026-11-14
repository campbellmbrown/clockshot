using static ClockShot.Platform.Windows.Interop.NativeMethods;

namespace ClockShot.Platform.Windows.Tests;

/// <summary>
/// Smoke tests on the real desktop.
/// Results depend on what the user is doing, so they check shape, not content.
/// Captured pixels are never written anywhere.
/// </summary>
public sealed class WindowsPlatformTests
{
    [Fact]
    public void SamplerReadsCurrentState()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var sample = new WindowsSystemSampler().Read(now);

        // Assert
        Assert.Equal(now, sample.TimeUtc);
        Assert.True(sample.IdleTime >= TimeSpan.Zero);
        if (sample.Window is { } window)
        {
            Assert.NotEqual(0, window.Handle);
            Assert.False(string.IsNullOrWhiteSpace(window.ProcessName));
            Assert.False(string.IsNullOrWhiteSpace(window.DisplayName));
        }
    }

    [Fact]
    public void CapturesForegroundWindowPixels()
    {
        // Arrange
        var window = new WindowsSystemSampler().Read(DateTime.UtcNow).Window;
        Assert.SkipWhen(window is null, "No foreground window. The session is locked or the desktop has focus.");
        Assert.SkipWhen(
            IsIconic(window.Handle) || !IsWindowVisible(window.Handle),
            "The foreground window is minimised or hidden.");

        // Act
        var image = new WindowsWindowCapturer().Capture(window.Handle);

        // Assert
        Assert.NotNull(image);
        Assert.True(image.Width > 0 && image.Height > 0);
    }

    [Fact]
    public void CaptureOfNoWindowReturnsNull()
    {
        // Act
        var image = new WindowsWindowCapturer().Capture(windowHandle: 0);

        // Assert
        Assert.Null(image);
    }
}
