namespace ClockShot.Platform.Windows.Tests;

/// <summary>
/// Smoke tests on the real desktop. Results depend on what the user is doing, so they check shape, not content.
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
            Assert.False(string.IsNullOrWhiteSpace(window.ProcessName));
            Assert.False(string.IsNullOrWhiteSpace(window.DisplayName));
        }
    }
}
