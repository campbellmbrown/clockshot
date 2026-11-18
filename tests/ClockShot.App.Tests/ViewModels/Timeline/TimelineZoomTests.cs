using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Models;

namespace ClockShot.App.Tests.ViewModels.Timeline;

public sealed class TimelineZoomTests
{
    private static readonly DateTime Midnight = new(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Day = new(Midnight, Midnight.AddDays(1));

    private readonly TimelineZoom _zoom = new();

    public TimelineZoomTests()
    {
        _zoom.Fit(Day);
        _zoom.SetWidth(1000);
    }

    [Fact]
    public void ZoomingInOneStepShowsLessTime()
    {
        // Act
        _zoom.ZoomAt(x: 500, steps: 1);

        // Assert
        // Each step divides the visible time by 1.5, so 24 hours become 16.
        Assert.Equal(16, _zoom.Viewport.Visible.Duration.TotalHours, precision: 6);
    }

    [Fact]
    public void ZoomingOutOneStepShowsMoreTime()
    {
        // Arrange
        _zoom.ZoomAt(x: 500, steps: 2);

        // Act
        _zoom.ZoomAt(x: 500, steps: -1);

        // Assert
        Assert.Equal(16, _zoom.Viewport.Visible.Duration.TotalHours, precision: 6);
    }

    [Fact]
    public void ShowingARangeZoomsToIt()
    {
        // Arrange
        var morning = new TimeRange(Midnight.AddHours(8), Midnight.AddHours(12));

        // Act
        _zoom.Show(morning);

        // Assert
        Assert.Equal(morning, _zoom.Viewport.Visible);
    }

    [Fact]
    public void ShowingARangeStillScrollsThroughTheWholeDay()
    {
        // Arrange
        _zoom.Show(new TimeRange(Midnight.AddHours(8), Midnight.AddHours(12)));

        // Act
        // Further left than the day goes, so the view stops at the day's start.
        _zoom.PanBy(pixels: -100_000);

        // Assert
        Assert.Equal(Day.Start, _zoom.Viewport.VisibleStart);
    }

    [Fact]
    public void FitShowsTheWholeDay()
    {
        // Arrange
        _zoom.ZoomAt(x: 500, steps: 2);

        // Act
        _zoom.Fit(Day);

        // Assert
        Assert.Equal(Day, _zoom.Viewport.Visible);
    }

    [Fact]
    public void ResizingTellsTheTimelineToRedraw()
    {
        // Arrange
        var changes = 0;
        _zoom.ViewportChanged += () => changes++;

        // Act
        _zoom.SetWidth(width: 500);

        // Assert
        Assert.Equal(1, changes);
    }

    [Fact]
    public void ChangingTheViewportTellsTheTimelineToRedraw()
    {
        // Arrange
        var changes = 0;
        _zoom.ViewportChanged += () => changes++;

        // Act
        _zoom.ZoomAt(x: 500, steps: 1);

        // Assert
        Assert.Equal(1, changes);
    }
}
