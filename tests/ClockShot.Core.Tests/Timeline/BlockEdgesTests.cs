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

    [Fact]
    public void TheEndStaysPutWhenTheStartIsNear()
    {
        // Act
        var anchor = BlockEdges.AnchorNear(First.Range, NineAm.AddSeconds(30), Tolerance);

        // Assert
        Assert.Equal(First.Range.End, anchor);
    }

    [Fact]
    public void TheStartStaysPutWhenTheEndIsNear()
    {
        // Act
        var anchor = BlockEdges.AnchorNear(First.Range, First.Range.End.AddSeconds(-30), Tolerance);

        // Assert
        Assert.Equal(First.Range.Start, anchor);
    }

    [Fact]
    public void TheEndStaysPutWhenBothEndsAreNear()
    {
        // Arrange
        var shortRange = new TimeRange(NineAm, NineAm.AddSeconds(30));

        // Act
        var anchor = BlockEdges.AnchorNear(shortRange, NineAm.AddSeconds(15), Tolerance);

        // Assert
        Assert.Equal(shortRange.End, anchor);
    }

    [Fact]
    public void NoEndIsNearTheMiddleOfARange()
    {
        // Act
        var anchor = BlockEdges.AnchorNear(First.Range, NineAm.AddMinutes(30), Tolerance);

        // Assert
        Assert.Null(anchor);
    }

    private static TimelineBlock Block(DateTime start, DateTime end) =>
        new(
            new TimeRange(start, end),
            Label: "Active",
            new StateColor(ComputerState.Active),
            Payload: ComputerState.Active);
}
