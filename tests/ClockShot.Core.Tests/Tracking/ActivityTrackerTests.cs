using ClockShot.Core.Models;
using ClockShot.Core.Tracking;

namespace ClockShot.Core.Tests.Tracking;

public sealed class ActivityTrackerTests
{
    private static readonly DateTime Start = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new("Editor.exe", @"C:\Editor.exe", "Editor", "DESIGN.md");
    private static readonly ForegroundWindow Browser = new("Browser.exe", @"C:\Browser.exe", "Browser", "Inbox");

    private static readonly TimeSpan AwayAfter = TimeSpan.FromMinutes(5);

    private readonly ActivityTracker _tracker = new();

    [Fact]
    public void FirstSampleStartsActiveStateAndActivity()
    {
        // Act
        var changes = Sample(0, Editor);

        // Assert
        Assert.Equal(
            [
                new StateStarted(At(0), ComputerState.Active),
                new ActivityStarted(At(0), Editor),
                new SegmentsExtended(At(0)),
            ],
            changes);
    }

    [Fact]
    public void SameWindowOnlyExtends()
    {
        // Arrange
        Sample(0, Editor);

        // Act
        var changes = Sample(1, Editor);

        // Assert
        Assert.Equal([new SegmentsExtended(At(1))], changes);
    }

    [Fact]
    public void ProcessNameThatDiffersOnlyByCaseIsTheSameApp()
    {
        // Arrange
        Sample(0, Editor);

        // Act
        var changes = Sample(1, Editor with { ProcessName = "EDITOR.EXE", ExecutablePath = @"C:\EDITOR.EXE" });

        // Assert
        Assert.Equal([new SegmentsExtended(At(1))], changes);
    }

    [Fact]
    public void WindowSwitchEndsActivityAndStartsNewOne()
    {
        // Arrange
        Sample(0, Editor);

        // Act
        var changes = Sample(1, Browser);

        // Assert
        Assert.Equal(
            [new ActivityEnded(At(1)), new ActivityStarted(At(1), Browser), new SegmentsExtended(At(1))],
            changes);
    }

    [Fact]
    public void TitleChangeStartsNewActivity()
    {
        // Arrange
        Sample(0, Editor);
        var otherFile = Editor with { Title = "TESTING.md" };

        // Act
        var changes = Sample(1, otherFile);

        // Assert
        Assert.Equal(
            [new ActivityEnded(At(1)), new ActivityStarted(At(1), otherFile), new SegmentsExtended(At(1))],
            changes);
    }

    [Fact]
    public void GoingAwayIsDatedFromLastInput()
    {
        // Arrange
        Sample(0, Editor);
        for (var second = 1; second < 400; second++)
        {
            Sample(second, Editor, idleSeconds: Math.Max(0, second - 100));
        }

        // Act
        // At 400 s the user has been idle for 300 s, which is 5 minutes. They're away from their last input at 100 s.
        var changes = Sample(400, Editor, idleSeconds: 300);

        // Assert
        Assert.Equal(
            [
                new ActivityEnded(At(100)),
                new StateEnded(At(100)),
                new StateStarted(At(100), ComputerState.Away),
                new SegmentsExtended(At(400)),
            ],
            changes);
        Assert.Null(_tracker.CurrentWindow);
    }

    [Fact]
    public void ComingBackFromAwayStartsActiveAtLastInput()
    {
        // Arrange
        Sample(0, Editor, idleSeconds: 600);

        // Act
        var changes = Sample(5, Browser, idleSeconds: 1);

        // Assert
        Assert.Equal(
            [
                new StateEnded(At(4)),
                new StateStarted(At(4), ComputerState.Active),
                new ActivityStarted(At(5), Browser),
                new SegmentsExtended(At(5)),
            ],
            changes);
    }

    [Fact]
    public void NoForegroundWindowEndsActivityButStaysActive()
    {
        // Arrange
        Sample(0, Editor);

        // Act
        var changes = Sample(1, window: null);

        // Assert
        Assert.Equal([new ActivityEnded(At(1)), new SegmentsExtended(At(1))], changes);
        Assert.Equal(ComputerState.Active, _tracker.CurrentState);
    }

    [Fact]
    public void StopEndsOpenSegments()
    {
        // Arrange
        Sample(0, Editor);

        // Act
        var changes = _tracker.Stop(At(5));

        // Assert
        Assert.Equal([new ActivityEnded(At(5)), new StateEnded(At(5))], changes);
    }

    [Fact]
    public void TheFirstSampleAfterStopStartsFresh()
    {
        // Arrange
        Sample(0, Editor);
        _tracker.Stop(At(5));

        // Act
        var changes = Sample(10, Editor);

        // Assert
        Assert.Equal(
            [
                new StateStarted(At(10), ComputerState.Active),
                new ActivityStarted(At(10), Editor),
                new SegmentsExtended(At(10)),
            ],
            changes);
    }

    [Fact]
    public void StopWithNothingOpenDoesNothing()
    {
        // Act
        var changes = _tracker.Stop(At(0));

        // Assert
        Assert.Empty(changes);
    }

    private static DateTime At(int seconds) => Start.AddSeconds(seconds);

    private IReadOnlyList<TrackingChange> Sample(int seconds, ForegroundWindow? window, int idleSeconds = 0)
    {
        var sample = new TrackingSample(At(seconds), window, TimeSpan.FromSeconds(idleSeconds));
        return _tracker.Process(sample, AwayAfter);
    }
}
