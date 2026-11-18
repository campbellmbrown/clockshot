using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.App.Tests.ViewModels.Timeline;

public sealed class TimelineSelectionTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Hour = new(NineAm, NineAm.AddHours(1));

    private readonly SelectionDetailsViewModel _details;
    private readonly TimelineSelection _selection;

    public TimelineSelectionTests()
    {
        _details = new SelectionDetailsViewModel();
        _selection = new TimelineSelection(_details);
    }

    [Fact]
    public void SelectingABlockShowsIt()
    {
        // Arrange
        var block = Block(new ComputerStateBlock(ComputerState.Away));

        // Act
        _selection.SelectBlock(LaneKind.Computer, block);

        // Assert
        Assert.Equal(Hour, _selection.Range);
        Assert.Equal(LaneKind.Computer, _selection.Lane);
        Assert.Equal("Away", _details.Heading);
        Assert.Equal("09:00 - 10:00", _details.TimeRangeText);
        Assert.Equal("1 h", _details.TimeText);
    }

    [Fact]
    public void SelectingTimeHasNoLane()
    {
        // Arrange
        _selection.SelectBlock(LaneKind.Computer, Block(new ComputerStateBlock(ComputerState.Away)));

        // Act
        _selection.SelectTime(Hour);

        // Assert
        Assert.Equal(Hour, _selection.Range);
        Assert.Null(_selection.Lane);
        Assert.Equal("Selected time", _details.Heading);
    }

    [Fact]
    public void ClearingRemovesTheSelectionAndItsDetails()
    {
        // Arrange
        _selection.SelectTime(Hour);

        // Act
        _selection.Clear();

        // Assert
        Assert.False(_selection.HasSelection);
        Assert.False(_details.HasSelection);
    }

    [Fact]
    public void OnlyTheSelectedLanesBlockIsSelected()
    {
        // Arrange
        var block = Block(new ComputerStateBlock(ComputerState.Away));
        _selection.SelectBlock(LaneKind.Computer, block);

        // Act
        var onItsLane = _selection.IsSelected(Lane(LaneKind.Computer, block), block);
        var onAnotherLane = _selection.IsSelected(Lane(LaneKind.Applications, block), block);

        // Assert
        Assert.True(onItsLane);
        Assert.False(onAnotherLane);
    }

    [Fact]
    public void RefreshingSelectsTheSameBlockOnItsLaneAgain()
    {
        // Arrange
        _selection.SelectBlock(LaneKind.Computer, Block(new ComputerStateBlock(ComputerState.Away)));
        var reloaded = Block(new ComputerStateBlock(ComputerState.Locked));

        // Act
        _selection.Refresh([Lane(LaneKind.Computer, reloaded)]);

        // Assert
        Assert.Equal(LaneKind.Computer, _selection.Lane);
        Assert.Equal("Locked", _details.Heading);
    }

    [Fact]
    public void RefreshingKeepsTimeThatNoBlockCovers()
    {
        // Arrange
        _selection.SelectTime(Hour);

        // Act
        _selection.Refresh([]);

        // Assert
        Assert.Equal(Hour, _selection.Range);
        Assert.Null(_selection.Lane);
    }

    [Fact]
    public void RefreshingKeepsTheTimeOfABlockThatIsGone()
    {
        // Arrange
        _selection.SelectBlock(LaneKind.Computer, Block(new ComputerStateBlock(ComputerState.Away)));

        // Act
        _selection.Refresh([]);

        // Assert
        Assert.Equal(Hour, _selection.Range);
        Assert.Null(_selection.Lane);
        Assert.Equal("Selected time", _details.Heading);
    }

    [Fact]
    public void RefreshingWithNothingSelectedSelectsNothing()
    {
        // Act
        _selection.Refresh([Lane(LaneKind.Computer, Block(new ComputerStateBlock(ComputerState.Away)))]);

        // Assert
        Assert.False(_selection.HasSelection);
    }

    [Fact]
    public void TheBlockBeingRecordedStaysSelectedWhileItGrows()
    {
        // Arrange
        var away = new ComputerStateBlock(ComputerState.Away);
        _selection.SelectBlock(LaneKind.Computer, Block(away));
        var grown = Grown(away);

        // Act
        var isSelected = _selection.IsSelected(Lane(LaneKind.Computer, grown), grown);

        // Assert
        Assert.True(isSelected);
    }

    [Fact]
    public void RefreshingFollowsABlockThatGrew()
    {
        // Arrange
        var away = new ComputerStateBlock(ComputerState.Away);
        _selection.SelectBlock(LaneKind.Computer, Block(away));
        var grown = Grown(away);

        // Act
        _selection.Refresh([Lane(LaneKind.Computer, grown)]);

        // Assert
        Assert.Equal(grown.Range, _selection.Range);
        Assert.Equal(LaneKind.Computer, _selection.Lane);
    }

    [Fact]
    public void ChangesTellTheTimelineToRedraw()
    {
        // Arrange
        var changes = 0;
        _selection.Changed += () => changes++;

        // Act
        _selection.SelectTime(Hour);
        _selection.Clear();

        // Assert
        Assert.Equal(2, changes);
    }

    /// <summary>
    /// The same block five minutes longer, as a tick draws the block being recorded.
    /// </summary>
    private static TimelineBlock Grown(object payload) =>
        new(new TimeRange(Hour.Start, Hour.End.AddMinutes(5)), Label: "label", new HexColor("#336699"), payload);

    private static TimelineBlock Block(object payload) =>
        new(Hour, Label: "label", new HexColor("#336699"), payload);

    private static TimelineLane Lane(LaneKind kind, TimelineBlock block) => new(kind, title: kind.ToString(), [block]);
}
