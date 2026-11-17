using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class TimelineLaneTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private static readonly TimelineBlock First = Block(NineAm, NineAm.AddHours(1));
    private static readonly TimelineBlock Second = Block(NineAm.AddHours(2), NineAm.AddHours(3));
    private static readonly TimelineLane Lane = new(LaneKind.Computer, title: "Computer", [First, Second]);

    [Fact]
    public void FindsTheBlockAtItsStart()
    {
        // Act
        var block = Lane.BlockAt(NineAm);

        // Assert
        Assert.Same(First, block);
    }

    [Fact]
    public void FindsTheBlockATimeFallsIn()
    {
        // Act
        var block = Lane.BlockAt(NineAm.AddHours(2).AddMinutes(30));

        // Assert
        Assert.Same(Second, block);
    }

    [Fact]
    public void FindsNoBlockAtItsEnd()
    {
        // Act
        var block = Lane.BlockAt(NineAm.AddHours(1));

        // Assert
        Assert.Null(block);
    }

    [Fact]
    public void ListsTheBlocksARangeOverlaps()
    {
        // Act
        var blocks = Lane.BlocksIn(new TimeRange(NineAm.AddMinutes(30), NineAm.AddHours(2).AddMinutes(1)));

        // Assert
        Assert.Equal([First, Second], blocks);
    }

    [Fact]
    public void LeavesOutBlocksARangeOnlyTouches()
    {
        // Act
        var blocks = Lane.BlocksIn(new TimeRange(NineAm.AddHours(1), NineAm.AddHours(2)));

        // Assert
        Assert.Empty(blocks);
    }

    private static TimelineBlock Block(DateTime start, DateTime end) =>
        new(
            new TimeRange(start, end),
            Label: "Active",
            new StateColor(ComputerState.Active),
            Payload: new ComputerStateBlock(ComputerState.Active));
}
