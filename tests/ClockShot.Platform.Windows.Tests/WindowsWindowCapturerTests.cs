using static ClockShot.Platform.Windows.Interop.NativeMethods;

namespace ClockShot.Platform.Windows.Tests;

public sealed class WindowsWindowCapturerTests
{
    // Two side-by-side screens of 1920 by 1080, with the left one at negative coordinates.
    private static readonly Rect Screens = new() { Left = -1920, Top = 0, Right = 1920, Bottom = 1080 };

    [Fact]
    public void AWindowOnTheScreensIsKeptWhole()
    {
        // Arrange
        var window = new Rect { Left = -100, Top = 50, Right = 300, Bottom = 400 };

        // Act
        var clipped = WindowsWindowCapturer.ClipToScreens(window, Screens);

        // Assert
        Assert.Equal(window, clipped);
    }

    [Fact]
    public void APartOffTheScreensIsCutAway()
    {
        // Arrange
        var window = new Rect { Left = 1800, Top = -20, Right = 2100, Bottom = 1200 };

        // Act
        var clipped = WindowsWindowCapturer.ClipToScreens(window, Screens);

        // Assert
        Assert.Equal(new Rect { Left = 1800, Top = 0, Right = 1920, Bottom = 1080 }, clipped);
    }

    [Fact]
    public void AWindowOffTheScreensHasNoArea()
    {
        // Arrange
        var window = new Rect { Left = 2000, Top = 100, Right = 2400, Bottom = 500 };

        // Act
        var clipped = WindowsWindowCapturer.ClipToScreens(window, Screens);

        // Assert
        Assert.True(clipped.Right <= clipped.Left);
    }
}
