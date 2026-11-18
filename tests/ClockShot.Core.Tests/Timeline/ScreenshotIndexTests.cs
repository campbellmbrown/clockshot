using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class ScreenshotIndexTests
{
    private const int ImageSize = 100;

    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    // Out of order, so the tests also show that the index sorts them.
    private readonly ScreenshotIndex _index = new([Shot(10), Shot(0), Shot(5), Shot(20)]);

    [Fact]
    public void FindsLatestAtOrBeforeTimeWithinRange()
    {
        // Act
        var found = _index.Find(At(7), Minutes(0, 15));

        // Assert
        Assert.Equal(5, found?.Id);
    }

    [Fact]
    public void ExactTimeMatches()
    {
        // Act
        var found = _index.Find(At(10), Minutes(0, 15));

        // Assert
        Assert.Equal(10, found?.Id);
    }

    [Fact]
    public void TheLastOfScreenshotsTakenAtTheSameTimeIsFound()
    {
        // Arrange
        // The sort keeps the given order for equal times, so the last one given wins.
        var sameTime = new Screenshot(Id: 99, At(10), "99.webp", ImageSize, ImageSize);
        var index = new ScreenshotIndex([Shot(10), sameTime]);

        // Act
        var found = index.Find(At(12), Minutes(0, 15));

        // Assert
        Assert.Equal(99, found?.Id);
    }

    [Fact]
    public void AScreenshotAtTheEndOfTheRangeIsLeftOut()
    {
        // Arrange
        var index = new ScreenshotIndex([Shot(5)]);

        // Act
        var found = index.Find(At(1), Minutes(0, 5));

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void FallsBackToFirstAfterWhenNoneBeforeInRange()
    {
        // Act
        var found = _index.Find(At(7), Minutes(6, 15));

        // Assert
        Assert.Equal(10, found?.Id);
    }

    [Fact]
    public void NullWhenRangeHasNoScreenshot()
    {
        // Act
        var found = _index.Find(At(12), Minutes(11, 19));

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void EmptyIndexFindsNothing()
    {
        // Act
        var found = ScreenshotIndex.Empty.Find(At(0), Minutes(0, 60));

        // Assert
        Assert.Null(found);
    }

    private static DateTime At(int minutes) => Nine.AddMinutes(minutes);

    private static TimeRange Minutes(int from, int to) => new(At(from), At(to));

    private static Screenshot Shot(int minutes) => new(minutes, At(minutes), $"{minutes}.webp", ImageSize, ImageSize);
}
