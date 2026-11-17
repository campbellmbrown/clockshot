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
    public void FitShowsTheWholeRange()
    {
        // Arrange
        var workingHours = new TimeRange(Midnight.AddHours(9), Midnight.AddHours(17));

        // Act
        _zoom.Fit(workingHours);

        // Assert
        Assert.Equal(workingHours, _zoom.Viewport.Visible);
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
}
