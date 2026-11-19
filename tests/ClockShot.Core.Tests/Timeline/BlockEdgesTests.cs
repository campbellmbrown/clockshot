using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class BlockEdgesTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(1);

    private static readonly TimelineBlock First = Block(NineAm, NineAm.AddHours(1));
    private static readonly TimelineBlock Second = Block(NineAm.AddHours(2), NineAm.AddHours(3));
    private static readonly TimelineLane Lane = new(LaneKind.Computer, title: "Computer", [First, Second]);

    [Fact]
    public void SnapsToTheNearestEdge()
    {
        // Arrange
        var pointer = NineAm.AddSeconds(15);
        var nearerEdge = NineAm.AddSeconds(20);
        var applications = new TimelineLane(LaneKind.Applications, title: "Applications", [Block(nearerEdge, NineAm.AddHours(1))]);

        // Act
        var snapped = BlockEdges.Snap([Lane, applications], pointer, Tolerance);

        // Assert
        Assert.Equal(nearerEdge, snapped);
    }

    [Fact]
    public void SnapsToAnEdgeExactlyTheToleranceAway()
    {
        // Act
        var snapped = BlockEdges.Snap([Lane], NineAm.AddHours(1) + Tolerance, Tolerance);

        // Assert
        Assert.Equal(NineAm.AddHours(1), snapped);
    }

    [Fact]
    public void KeepsTheTimeWhenNoEdgeIsNear()
    {
        // Arrange
        var time = NineAm.AddHours(1) + Tolerance + TimeSpan.FromTicks(1);

        // Act
        var snapped = BlockEdges.Snap([Lane], time, Tolerance);

        // Assert
        Assert.Equal(time, snapped);
    }

    private static TimelineBlock Block(DateTime start, DateTime end) =>
        new(
            new TimeRange(start, end),
            Label: "Active",
            new StateColor(ComputerState.Active),
            Payload: ComputerState.Active);
}
