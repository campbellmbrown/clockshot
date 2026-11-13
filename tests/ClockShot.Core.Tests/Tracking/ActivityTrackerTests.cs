using ClockShot.Core.Models;
using ClockShot.Core.Tracking;

namespace ClockShot.Core.Tests.Tracking;

public sealed class ActivityTrackerTests
{
    private static readonly DateTime Start = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new("Editor.exe", @"C:\Editor.exe", "Editor", "notes.txt");
    private static readonly ForegroundWindow Browser = new("Browser.exe", @"C:\Browser.exe", "Browser", "Inbox");

    private static readonly TimeSpan AwayAfter = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxSampleGap = TimeSpan.FromSeconds(10);

    private readonly ActivityTracker _tracker = new(MaxSampleGap);

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
    public void LockingEndsActivityAtLockTime()
    {
        // Arrange
        Sample(0, Editor);

        // Act
        var changes = Sample(1, Editor, isLocked: true);

        // Assert
        Assert.Equal(
            [
                new ActivityEnded(At(1)),
                new StateEnded(At(1)),
                new StateStarted(At(1), ComputerState.Locked),
                new SegmentsExtended(At(1)),
            ],
            changes);
    }

    [Fact]
    public void LockingWhileAwayKeepsTheAwayTime()
    {
        // Arrange
        // The user stops at 100 s, is away from 100 s, and the screen locks itself at 600 s.
        for (var second = 0; second < 600; second++)
        {
            Sample(second, Editor, idleSeconds: Math.Max(0, second - 100));
        }

        // Act
        var changes = Sample(600, Editor, idleSeconds: 500, isLocked: true);

        // Assert
        Assert.Equal(
            [new StateEnded(At(600)), new StateStarted(At(600), ComputerState.Locked), new SegmentsExtended(At(600))],
            changes);
    }

    [Fact]
    public void UnlockingWithoutInputKeepsTheLockedTime()
    {
        // Arrange
        for (var second = 0; second < 10; second++)
        {
            Sample(second, Editor, idleSeconds: 400 + second, isLocked: true);
        }

        // Act
        var changes = Sample(10, Editor, idleSeconds: 410);

        // Assert
        Assert.Equal(
            [new StateEnded(At(10)), new StateStarted(At(10), ComputerState.Away), new SegmentsExtended(At(10))],
            changes);
    }

    [Fact]
    public void LongGapBetweenSamplesEndsEverythingAtLastSample()
    {
        // Arrange
        Sample(0, Editor);
        Sample(1, Editor);

        // Act
        var changes = Sample(3600, Editor);

        // Assert
        Assert.Equal(
            [
                new ActivityEnded(At(1)),
                new StateEnded(At(1)),
                new StateStarted(At(3600), ComputerState.Active),
                new ActivityStarted(At(3600), Editor),
                new SegmentsExtended(At(3600)),
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

    private IReadOnlyList<TrackingChange> Sample(
        int seconds,
        ForegroundWindow? window,
        int idleSeconds = 0,
        bool isLocked = false)
    {
        var sample = new TrackingSample(At(seconds), window, TimeSpan.FromSeconds(idleSeconds), isLocked);
        return _tracker.Process(sample, AwayAfter);
    }
}
