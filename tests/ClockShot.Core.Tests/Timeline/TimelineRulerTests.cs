using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class TimelineRulerTests
{
    // One pixel per minute when the whole day is shown, which keeps the spacing easy to work out.
    private const double Width = 24 * 60;

    private static readonly DateTime Midnight = new(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Day = new(Midnight, Midnight.AddDays(1));

    [Fact]
    public void WholeDayOnWideScreenLabelsEveryTwoHours()
    {
        // Act
        var ticks = TimelineRuler.Ticks(TimelineViewport.Fit(Day, Width), TimeZoneInfo.Utc);

        // Assert
        string[] expected =
        [
            "00:00", "02:00", "04:00", "06:00", "08:00", "10:00", "12:00",
            "14:00", "16:00", "18:00", "20:00", "22:00", "00:00",
        ];
        Assert.Equal(expected, Labels(ticks));
        Assert.Equal(TimeSpan.FromMinutes(30), ticks[1].TimeUtc - ticks[0].TimeUtc);
    }

    [Fact]
    public void ZoomedInUsesFinerTicks()
    {
        // Arrange
        // One hour across the width is 2.5 seconds a pixel.
        // Two-minute labels would be 48 pixels apart, which is too close, so labels are five minutes apart.
        var hour = new TimeRange(Midnight.AddHours(9), Midnight.AddHours(10));
        var viewport = TimelineViewport.Fit(hour, Width);

        // Act
        var ticks = TimelineRuler.Ticks(viewport, TimeZoneInfo.Utc);

        // Assert
        Assert.Equal(["09:00", "09:05", "09:10"], Labels(ticks).Take(3));
    }

    [Fact]
    public void TicksAreAlignedToLocalClock()
    {
        // Arrange
        var zone = TestTimeZones.HalfHourOffset;

        // Act
        var ticks = TimelineRuler.Ticks(TimelineViewport.Fit(Day, Width), zone);

        // Assert
        var labelled = ticks.Where(tick => tick.Label is not null).ToList();
        Assert.NotEmpty(labelled);
        Assert.All(labelled, tick => Assert.Equal(0, TimeZoneInfo.ConvertTimeFromUtc(tick.TimeUtc, zone).Minute));
    }

    [Fact]
    public void AllTicksAreInsideVisibleRange()
    {
        // Arrange
        var viewport = TimelineViewport.Fit(Day, Width).ZoomAt(x: 500, factor: 7.3).PanBy(pixels: 123);

        // Act
        var ticks = TimelineRuler.Ticks(viewport, TimeZoneInfo.Utc);

        // Assert
        Assert.All(ticks, tick => Assert.InRange(tick.TimeUtc, viewport.VisibleStart, viewport.VisibleEnd));
    }

    [Fact]
    public void SkipsTheHourMissingWhenClocksGoForward()
    {
        // Arrange
        // Local 00:00 to 06:00 on 29 March 2026, when the clocks jump from 02:00 to 03:00.
        var night = new TimeRange(
            new DateTime(2026, 3, 28, 23, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 29, 4, 0, 0, DateTimeKind.Utc));

        // Act
        var ticks = TimelineRuler.Ticks(TimelineViewport.Fit(night, Width), TestTimeZones.WithDaylightSaving);

        // Assert
        var labels = Labels(ticks);
        Assert.Contains("01:45", labels);
        Assert.Contains("03:00", labels);
        Assert.DoesNotContain(labels, label => label.StartsWith("02:", StringComparison.Ordinal));
    }

    [Fact]
    public void TicksStayInsideTheRangeWhenClocksGoBack()
    {
        // Arrange
        // Ends at local 02:30 on 25 October 2026, before the clocks go back from 03:00 to 02:00.
        // The ruler treats a repeated local time as the later one, so local 02:00 is after this range.
        var night = new TimeRange(
            new DateTime(2026, 10, 24, 23, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc));
        var viewport = TimelineViewport.Fit(night, Width);

        // Act
        var ticks = TimelineRuler.Ticks(viewport, TestTimeZones.WithDaylightSaving);

        // Assert
        Assert.NotEmpty(ticks);
        Assert.All(ticks, tick => Assert.InRange(tick.TimeUtc, viewport.VisibleStart, viewport.VisibleEnd));
    }

    private static List<string> Labels(IEnumerable<RulerTick> ticks) =>
        [.. ticks.Select(tick => tick.Label).OfType<string>()];
}
