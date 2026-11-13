namespace ClockShot.Platform.Windows.Tests;

public sealed class WindowsSystemSamplerTests
{
    [Fact]
    public void DesktopNameReadsOnlyWhatWindowsWrote()
    {
        // Arrange
        var buffer = "Winlogon\0Leftover".ToCharArray();
        var bytesWritten = "Winlogon\0".Length * sizeof(char);

        // Act
        var name = WindowsSystemSampler.DesktopName(buffer, bytesWritten);

        // Assert
        Assert.Equal("Winlogon", name);
    }

    [Fact]
    public void DesktopNameNeverReadsPastTheBuffer()
    {
        // Arrange
        var buffer = "Default".ToCharArray();

        // Act
        var name = WindowsSystemSampler.DesktopName(buffer, bytesWritten: 1000);

        // Assert
        Assert.Equal("Default", name);
    }

    [Theory]
    [InlineData("Default", false)]
    [InlineData("default", false)]
    [InlineData("Winlogon", true)]
    [InlineData("Screen-saver", true)]
    public void OnlyTheNormalDesktopIsUnlocked(string desktopName, bool expected)
    {
        // Act
        var isLocked = WindowsSystemSampler.IsLockScreen(desktopName);

        // Assert
        Assert.Equal(expected, isLocked);
    }
}
