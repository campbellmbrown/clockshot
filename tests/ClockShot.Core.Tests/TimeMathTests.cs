using ClockShot.Core.Models;

namespace ClockShot.Core.Tests;

public sealed class TimeMathTests
{
    [Fact]
    public void SumAddsTheDurations()
    {
        // Arrange
        TimeSpan[] durations = [TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5)];

        // Act
        var total = durations.Sum();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(15), total);
    }

    [Fact]
    public void SumOfNoDurationsIsZero()
    {
        // Act
        var total = Array.Empty<TimeSpan>().Sum();

        // Assert
        Assert.Equal(TimeSpan.Zero, total);
    }
}
