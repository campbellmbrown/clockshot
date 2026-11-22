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
    public void AllCoversEveryRange()
    {
        // Arrange
        var range = new TimeRange(At(9), At(10));

        // Act
        var overlap = TimeRange.All.Intersect(range);

        // Assert
        Assert.Equal(range, overlap);
    }

    [Fact]
    public void RejectsAnEndBeforeTheStart()
    {
        // Act
        var exception = Record.Exception(() => new TimeRange(At(10), At(9)));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(9, 0, true)]
    [InlineData(9, 59, true)]
    [InlineData(10, 0, false)]
    public void ContainsItsStartButNotItsEnd(int hour, int minute, bool expected)
    {
        // Arrange
        var range = new TimeRange(At(9), At(10));

        // Act
        var contains = range.Contains(At(hour, minute));

        // Assert
        Assert.Equal(expected, contains);
    }

    [Fact]
    public void RangesThatShareTimeOverlap()
    {
        // Act
        var overlaps = new TimeRange(At(9), At(10)).Overlaps(new TimeRange(At(9, 30), At(11)));

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void TouchingRangesDoNotOverlap()
    {
        // Act
        var overlaps = new TimeRange(At(9), At(10)).Overlaps(new TimeRange(At(10), At(11)));

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void IntersectReturnsTheSharedPart()
    {
        // Act
        var shared = new TimeRange(At(9), At(10)).Intersect(new TimeRange(At(9, 30), At(11)));

        // Assert
        Assert.Equal(new TimeRange(At(9, 30), At(10)), shared);
    }

    [Fact]
    public void TouchingRangesHaveNoIntersection()
    {
        // Act
        var shared = new TimeRange(At(9), At(10)).Intersect(new TimeRange(At(10), At(11)));

        // Assert
        Assert.Null(shared);
    }

    [Fact]
    public void CoverSpansBothRangesAndTheGapBetweenThem()
    {
        // Act
        var cover = new TimeRange(At(9), At(10)).Cover(new TimeRange(At(11), At(12)));

        // Assert
        Assert.Equal(new TimeRange(At(9), At(12)), cover);
    }
}
