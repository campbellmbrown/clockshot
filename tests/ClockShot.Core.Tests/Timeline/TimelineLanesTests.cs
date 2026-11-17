using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class TimelineLanesTests
{
    // The lanes never read segment ids, so every record shares one.
    private const long AnyId = 0;

    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange FirstHour = new(Nine, Nine.AddHours(1));

    [Fact]
    public void EachComputerStateBecomesABlock()
    {
        // Arrange
        var records = ActivityRecords.Empty with { States = [new(AnyId, FirstHour, ComputerState.Away)] };

        // Act
        var lane = Lane(records, LaneKind.Computer);

        // Assert
        var block = Assert.Single(lane.Blocks);
        var expected = new TimelineBlock(
            FirstHour,
            Label: "Away",
            new StateColor(ComputerState.Away),
            Payload: new ComputerStateBlock(ComputerState.Away));
        Assert.Equal(expected, block);
    }

    private static TimelineLane Lane(ActivityRecords records, LaneKind kind) =>
        TimelineLanes.Build(records).Single(lane => lane.Kind == kind);
}
