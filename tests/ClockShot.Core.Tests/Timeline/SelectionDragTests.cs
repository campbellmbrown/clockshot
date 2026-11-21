using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class SelectionDragTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly SelectionDrag Drag = new(Anchor: NineAm, ResizedTag: null);

    [Fact]
    public void RangeRunsFromTheAnchorToAPointerAfterIt()
    {
        // Act
        var range = Drag.RangeTo(NineAm.AddHours(1));

        // Assert
        Assert.Equal(new TimeRange(NineAm, NineAm.AddHours(1)), range);
    }

    [Fact]
    public void RangeRunsFromAPointerBeforeTheAnchorToIt()
    {
        // Act
        var range = Drag.RangeTo(NineAm.AddHours(-1));

        // Assert
        Assert.Equal(new TimeRange(NineAm.AddHours(-1), NineAm), range);
    }

    [Fact]
    public void ThereIsNoRangeWhenThePointerIsOnTheAnchor()
    {
        // Act
        var range = Drag.RangeTo(NineAm);

        // Assert
        Assert.Null(range);
    }
}
