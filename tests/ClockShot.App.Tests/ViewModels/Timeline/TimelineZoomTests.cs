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
    public void TypingALengthShowsThatMuchTime()
    {
        // Act
        _zoom.ShowDurationCommand.Execute("90 min");

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(90), _zoom.Viewport.Visible.Duration);
        Assert.Equal("1 h 30 min", _zoom.VisibleDurationText);
    }

    [Fact]
    public void UnreadableTextPutsTheCurrentLengthBack()
    {
        // Arrange
        var before = _zoom.Viewport;
        var refreshed = new List<string?>();
        _zoom.PropertyChanged += (_, e) => refreshed.Add(e.PropertyName);

        // Act
        _zoom.ShowDurationCommand.Execute("soon");

        // Assert
        Assert.Equal(before, _zoom.Viewport);
        Assert.Contains(nameof(TimelineZoom.VisibleDurationText), refreshed);
    }

    [Fact]
    public void ALengthTheLimitsChangeShowsTheLengthAllowed()
    {
        // Arrange
        var refreshed = new List<string?>();
        _zoom.PropertyChanged += (_, e) => refreshed.Add(e.PropertyName);

        // Act
        // The whole day is already shown, so 48 hours is held to the same 24.
        _zoom.ShowDurationCommand.Execute("48 h");

        // Assert
        Assert.Contains(nameof(TimelineZoom.VisibleDurationText), refreshed);
        Assert.Equal("24 h", _zoom.VisibleDurationText);
    }

    [Fact]
    public void ShowingLongerStepsUpToTheNextLength()
    {
        // Arrange
        _zoom.ShowDurationCommand.Execute("40 min");

        // Act
        _zoom.ShowLongerDuration();

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), _zoom.Viewport.Visible.Duration);
    }

    [Fact]
    public void ShowingShorterStepsDownToTheNextLength()
    {
        // Act
        _zoom.ShowShorterDuration();

        // Assert
        Assert.Equal(TimeSpan.FromHours(12), _zoom.Viewport.Visible.Duration);
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
