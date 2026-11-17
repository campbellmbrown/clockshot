using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class ActivitySummaryTests
{
    // The summary never reads segment ids, so every segment shares one.
    private const long AnyId = 0;

    private const long EditorId = 1;
    private const long BrowserId = 2;

    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ByApplicationSumsOnlyTheOverlap()
    {
        // Arrange
        ActivitySegment[] activities =
        [
            Activity(0, 30, EditorId),
            Activity(30, 40, BrowserId),
            Activity(40, 60, EditorId),
        ];

        // Act
        var totals = ActivitySummary.ByApplication(activities, Minutes(10, 50));

        // Assert
        ApplicationTotal[] expected =
        [
            new(EditorId, TimeSpan.FromMinutes(30)),
            new(BrowserId, TimeSpan.FromMinutes(10)),
        ];
        Assert.Equal(expected, totals);
    }

    [Fact]
    public void ByApplicationPutsTheLongestFirst()
    {
        // Arrange
        ActivitySegment[] activities = [Activity(0, 10, BrowserId), Activity(10, 40, EditorId)];

        // Act
        var totals = ActivitySummary.ByApplication(activities, Minutes(0, 60));

        // Assert
        Assert.Equal([EditorId, BrowserId], totals.Select(total => total.ApplicationId));
    }

    [Fact]
    public void TimeInCountsOneState()
    {
        // Arrange
        ComputerStateSegment[] states =
        [
            State(0, 30, ComputerState.Active),
            State(30, 45, ComputerState.Away),
            State(45, 60, ComputerState.Active),
        ];

        // Act
        var active = ActivitySummary.TimeIn(states, ComputerState.Active, Minutes(0, 60));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(45), active);
    }

    [Fact]
    public void TimeInCountsOnlyTheOverlap()
    {
        // Arrange
        ComputerStateSegment[] states = [State(0, 30, ComputerState.Active)];

        // Act
        var active = ActivitySummary.TimeIn(states, ComputerState.Active, Minutes(20, 60));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(10), active);
    }

    [Fact]
    public void TimeInIsZeroWithoutSegments()
    {
        // Act
        var active = ActivitySummary.TimeIn([], ComputerState.Active, Minutes(0, 60));

        // Assert
        Assert.Equal(TimeSpan.Zero, active);
    }

    [Fact]
    public void WorkingDayRunsFromFirstToLastActiveTime()
    {
        // Arrange
        ComputerStateSegment[] states =
        [
            State(-30, 0, ComputerState.Locked),
            State(0, 30, ComputerState.Active),
            State(30, 45, ComputerState.Away),
            State(45, 90, ComputerState.Active),
            State(90, 120, ComputerState.Locked),
        ];

        // Act
        var workingDay = ActivitySummary.WorkingDay(states, Minutes(-60, 200));

        // Assert
        Assert.Equal(Minutes(0, 90), workingDay);
    }

    [Fact]
    public void WorkingDayIsClippedToRange()
    {
        // Arrange
        ComputerStateSegment[] states = [State(0, 30, ComputerState.Active)];

        // Act
        var workingDay = ActivitySummary.WorkingDay(states, Minutes(10, 20));

        // Assert
        Assert.Equal(Minutes(10, 20), workingDay);
    }

    [Fact]
    public void NoActiveTimeMeansNoWorkingDay()
    {
        // Arrange
        ComputerStateSegment[] states = [State(0, 30, ComputerState.Locked)];

        // Act
        var workingDay = ActivitySummary.WorkingDay(states, Minutes(0, 60));

        // Assert
        Assert.Null(workingDay);
    }

    private static TimeRange Minutes(int from, int to) => new(Nine.AddMinutes(from), Nine.AddMinutes(to));

    private static ActivitySegment Activity(int from, int to, long applicationId) =>
        new(AnyId, Minutes(from, to), applicationId, WindowTitle: "");

    private static ComputerStateSegment State(int from, int to, ComputerState state) =>
        new(AnyId, Minutes(from, to), state);
}
