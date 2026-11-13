using ClockShot.Core.Models;

namespace ClockShot.Core.Tests;

public sealed class TimeRangeTests
{
    private static DateTime At(int hour, int minute = 0) => new(2026, 11, 10, hour, minute, 0, DateTimeKind.Utc);

    [Fact]
    public void RejectsLocalTimes()
    {
        // Arrange
        var start = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Local);

        // Act
        var exception = Record.Exception(() => new TimeRange(start, start.AddHours(1)));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void RejectsAnEndBeforeTheStart()
    {
        // Act
        var exception = Record.Exception(() => new TimeRange(At(10), At(9)));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }
}
