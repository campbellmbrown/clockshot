using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class ActivitySummaryTests
{
    // The summary never reads ids, so every segment shares one.
    private const long AnyId = 0;

    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

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

    private static ComputerStateSegment State(int from, int to, ComputerState state) =>
        new(AnyId, Minutes(from, to), state);
}
