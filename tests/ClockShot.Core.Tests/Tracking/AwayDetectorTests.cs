using ClockShot.Core.Models;
using ClockShot.Core.Tracking;

namespace ClockShot.Core.Tests.Tracking;

public sealed class AwayDetectorTests
{
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new(
        Handle: 1,
        ProcessName: "Editor.exe",
        ExecutablePath: null,
        DisplayName: "Editor",
        Title: "a.cs");

    private static readonly TimeSpan AwayAfter = TimeSpan.FromMinutes(5);

    // Samples here are minutes apart, so the sleep limit is 30 minutes instead of the loop's 10 seconds.
    private readonly ActivityTracker _tracker = new(maxSampleGap: TimeSpan.FromMinutes(30));
    private readonly AwayDetector _detector = new();

    [Fact]
    public void ReportsAwayTimeWhenTheUserComesBack()
    {
        // Arrange
        Sample(0);

        // Act
        // Twenty idle minutes at 09:30 mean the user has been away since 09:10.
        var whileAway = Sample(30, idleMinutes: 20);
        var onReturn = Sample(50);
        var afterReturn = Sample(51);

        // Assert
        Assert.Null(whileAway);
        Assert.Equal(Minutes(10, 50), onReturn);
        Assert.Null(afterReturn);
    }

    [Fact]
    public void LockedCountsAsAway()
    {
        // Arrange
        Sample(0);
        Sample(1, locked: true);

        // Act
        var onReturn = Sample(25);

        // Assert
        Assert.Equal(Minutes(1, 25), onReturn);
    }

    [Fact]
    public void AwayThenLockedIsOneAbsence()
    {
        // Arrange
        Sample(0);
        // Ten idle minutes at 09:20 mean the user has been away since 09:10.
        Sample(20, idleMinutes: 10);
        Sample(25, idleMinutes: 15, locked: true);

        // Act
        var onReturn = Sample(50);

        // Assert
        Assert.Equal(Minutes(10, 50), onReturn);
    }

    [Fact]
    public void AGapInTrackingCountsAsAway()
    {
        // Arrange
        Sample(0);
        Sample(1);

        // Act
        // No sample came for 89 minutes, so the computer was asleep from 09:01.
        var onReturn = Sample(90);

        // Assert
        Assert.Equal(Minutes(1, 90), onReturn);
    }

    [Fact]
    public void ResetForgetsTheAbsence()
    {
        // Arrange
        Sample(0);
        Sample(1, locked: true);

        // Act
        _detector.Reset();

        // Assert
        Assert.Null(Sample(25));
    }

    private static TimeRange Minutes(int from, int to) => new(Nine.AddMinutes(from), Nine.AddMinutes(to));

    private TimeRange? Sample(int minutes, int idleMinutes = 0, bool locked = false)
    {
        var sample = new TrackingSample(Nine.AddMinutes(minutes), Editor, TimeSpan.FromMinutes(idleMinutes), locked);
        return _detector.Observe(_tracker.Process(sample, AwayAfter));
    }
}
