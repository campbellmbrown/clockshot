using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class TimelineViewportTests
{
    // One pixel per minute when the whole day is shown, which keeps the numbers easy to check.
    private const double MinutesInADay = 24 * 60;

    private static readonly DateTime Midnight = new(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Day = new(Midnight, Midnight.AddDays(1));

    [Fact]
    public void FitShowsWholeDay()
    {
        // Act
        var viewport = Fitted();

        // Assert
        Assert.Equal(Day, viewport.Visible);
        Assert.Equal(60, viewport.SecondsPerPixel);
        Assert.True(viewport.IsFit);
    }

    [Fact]
    public void FitTreatsAZeroWidthAsOnePixel()
    {
        // Act
        var viewport = TimelineViewport.Fit(Day, width: 0);

        // Assert
        Assert.Equal(1, viewport.Width);
        Assert.Equal(Day, viewport.Visible);
    }

    [Fact]
    public void XAndTimeAreInverse()
    {
        // Arrange
        var viewport = Fitted();
        var nineAm = Midnight.AddHours(9);

        // Act
        var x = viewport.XOf(nineAm);

        // Assert
        Assert.Equal(540, x);
        Assert.Equal(nineAm, viewport.TimeAt(x));
    }

    [Fact]
    public void TimeAtCutsToTheMillisecond()
    {
        // Arrange
        var viewport = Fitted();

        // Act
        var time = viewport.TimeAt(x: 0.123456);

        // Assert
        Assert.Equal(Midnight.AddMilliseconds(7407), time);
    }

    [Fact]
    public void ZoomKeepsTimeUnderPointerInPlace()
    {
        // Act
        var zoomed = Fitted().ZoomAt(x: 540, factor: 4);

        // Assert
        Assert.Equal(15, zoomed.SecondsPerPixel);
        Assert.Equal(Midnight.AddHours(9), zoomed.TimeAt(x: 540));
    }

    [Fact]
    public void ZoomStopsAtOneMinuteAcross()
    {
        // Act
        var zoomed = Fitted().ZoomAt(x: 720, factor: 100_000);

        // Assert
        var noon = Midnight.AddHours(12);
        Assert.Equal(new TimeRange(noon.AddSeconds(-30), noon.AddSeconds(30)), zoomed.Visible);
    }

    [Fact]
    public void ZoomOutStopsAtWholeDay()
    {
        // Arrange
        var zoomedIn = Fitted().ZoomAt(x: 720, factor: 4);

        // Act
        var zoomedOut = zoomedIn.ZoomAt(x: 100, factor: 0.001);

        // Assert
        Assert.Equal(Day, zoomedOut.Visible);
    }

    [Fact]
    public void PanMovesTheViewByTheTimeThePixelsCover()
    {
        // Act
        var panned = FirstSixHours().PanBy(pixels: 240);

        // Assert
        Assert.Equal(Midnight.AddHours(1), panned.VisibleStart);
    }

    [Fact]
    public void PanStopsAtTheStartOfTheDay()
    {
        // Act
        var panned = FirstSixHours().PanBy(pixels: -1000);

        // Assert
        Assert.Equal(Midnight, panned.VisibleStart);
    }

    [Fact]
    public void PanStopsAtTheEndOfTheDay()
    {
        // Act
        var panned = FirstSixHours().PanBy(pixels: 1_000_000);

        // Assert
        Assert.Equal(Day.End, panned.VisibleEnd);
    }

    [Fact]
    public void CenterOnPutsTheTimeInTheMiddle()
    {
        // Act
        var centred = FirstSixHours().CenterOn(Midnight.AddHours(12));

        // Assert
        Assert.Equal(new TimeRange(Midnight.AddHours(9), Midnight.AddHours(15)), centred.Visible);
    }

    [Fact]
    public void CenterOnNearEndIsClamped()
    {
        // Act
        var centred = FirstSixHours().CenterOn(Midnight.AddHours(23));

        // Assert
        Assert.Equal(Day.End, centred.VisibleEnd);
    }

    [Fact]
    public void ShowRangeShowsExactlyThatRange()
    {
        // Arrange
        var range = new TimeRange(Midnight.AddHours(9), Midnight.AddHours(11));

        // Act
        var shown = Fitted().ShowRange(range);

        // Assert
        Assert.Equal(range, shown.Visible);
    }

    [Fact]
    public void ShowRangeShorterThanTheZoomLimitIsShownFromItsStart()
    {
        // Arrange
        var range = new TimeRange(Midnight.AddHours(9), Midnight.AddHours(9).AddSeconds(30));

        // Act
        var shown = Fitted().ShowRange(range);

        // Assert
        Assert.Equal(range.Start, shown.VisibleStart);
        Assert.Equal(TimelineViewport.MinVisibleDuration.TotalSeconds, shown.Visible.Duration.TotalSeconds, precision: 3);
    }

    [Fact]
    public void ResizingFittedViewStaysFitted()
    {
        // Act
        var resized = Fitted().WithWidth(720);

        // Assert
        Assert.Equal(Day, resized.Visible);
    }

    [Fact]
    public void ResizingZoomedViewKeepsStartAndZoom()
    {
        // Arrange
        var zoomed = FirstSixHours().PanBy(pixels: 240);

        // Act
        var resized = zoomed.WithWidth(720);

        // Assert
        Assert.Equal(zoomed.VisibleStart, resized.VisibleStart);
        Assert.Equal(zoomed.SecondsPerPixel, resized.SecondsPerPixel);
    }

    private static TimelineViewport Fitted() => TimelineViewport.Fit(Day, MinutesInADay);

    /// <summary>
    /// Zoomed in by a factor of 4 at the left edge, so it shows midnight to 6 am.
    /// </summary>
    private static TimelineViewport FirstSixHours() => Fitted().ZoomAt(x: 0, factor: 4);
}
