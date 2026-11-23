using ClockShot.App.Services;
using ClockShot.App.ViewModels.Tagging;
using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Data.Projects;
using ClockShot.Data.Tagging;

using NSubstitute;

namespace ClockShot.App.Tests.ViewModels.Timeline;

public sealed class TimelineSelectionTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Hour = new(NineAm, NineAm.AddHours(1));
    private static readonly TimeRange SecondHalf = new(NineAm.AddMinutes(30), NineAm.AddHours(1));
    private static readonly TimeEntry Tag = new(Id: 1, Hour, ProjectId: 10, TaskId: null, Note: null);

    private readonly SelectionDetailsViewModel _details;
    private readonly TimelineSelection _selection;

    public TimelineSelectionTests()
    {
        // These tests never load or save projects or tags.
        var database = UnopenedDatabase.Create();
        var entries = new TimeEntryRepository(database);
        var tagEditor = new TagEditorViewModel(
            new ProjectRepository(database),
            entries,
            new Tagger(entries),
            Substitute.For<IDialogService>());
        _details = new SelectionDetailsViewModel(tagEditor);
        var catalog = new ProjectCatalog([], [new Project(Tag.ProjectId, ClientId: null, ParentId: null, "Website", IsArchived: false)], []);
        _details.SetDay(ActivityRecords.Empty, catalog, TimeZoneInfo.Utc);
        _selection = new TimelineSelection(_details);
    }

    [Fact]
    public void SelectingABlockShowsItAndGetsReadyToTagIt()
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
        Assert.Equal(Hour, _details.TagEditor.Target);
        Assert.Null(_details.TagEditor.Editing);
    }

    [Fact]
    public void SelectingATagEditsIt()
    {
        // Act
        _selection.SelectBlock(LaneKind.Tags, Block(new TagBlock(Tag)));

        // Assert
        Assert.Equal(Tag, _details.TagEditor.Editing);
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
        Assert.Null(_details.TagEditor.Target);
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
    public void RefreshingFindsATagThatNowCoversTheSelectedTime()
    {
        // Arrange
        _selection.SelectTime(Hour);

        // Act
        _selection.Refresh([Lane(LaneKind.Tags, Block(new TagBlock(Tag)))]);

        // Assert
        Assert.Equal(LaneKind.Tags, _selection.Lane);
        Assert.Equal(Tag, _details.TagEditor.Editing);
    }

    [Fact]
    public void RefreshingAfterTaggingFindsATagJoinedWithItsNeighbour()
    {
        // Arrange
        _selection.SelectTime(SecondHalf);
        _selection.ExpectTag();

        // Act
        _selection.Refresh([Lane(LaneKind.Tags, Block(new TagBlock(Tag)))]);

        // Assert
        Assert.Equal(Hour, _selection.Range);
        Assert.Equal(Tag, _details.TagEditor.Editing);
    }

    [Fact]
    public void RefreshingKeepsTimeInsideATagThatWasNotJustTagged()
    {
        // Arrange
        _selection.SelectTime(SecondHalf);

        // Act
        _selection.Refresh([Lane(LaneKind.Tags, Block(new TagBlock(Tag)))]);

        // Assert
        Assert.Equal(SecondHalf, _selection.Range);
        Assert.Null(_selection.Lane);
    }

    [Fact]
    public void RefreshingKeepsTimeThatNoBlockCovers()
    {
        // Arrange
        _selection.SelectTime(Hour);
        _details.TagEditor.Clear();

        // Act
        _selection.Refresh([]);

        // Assert
        Assert.Equal(Hour, _selection.Range);
        Assert.Null(_selection.Lane);
        Assert.Equal(Hour, _details.TagEditor.Target);
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
    public void RefreshingKeepsUnsavedChangesToAnUnchangedTag()
    {
        // Arrange
        _selection.SelectBlock(LaneKind.Tags, Block(new TagBlock(Tag)));
        _details.TagEditor.Note = "Not saved yet";

        // Act
        _selection.Refresh([Lane(LaneKind.Tags, Block(new TagBlock(Tag)))]);

        // Assert
        Assert.Equal("Not saved yet", _details.TagEditor.Note);
    }

    [Fact]
    public void RefreshingShowsATagThatWasSaved()
    {
        // Arrange
        _selection.SelectBlock(LaneKind.Tags, Block(new TagBlock(Tag)));
        var saved = Tag with { Note = "Saved" };

        // Act
        _selection.Refresh([Lane(LaneKind.Tags, Block(new TagBlock(saved)))]);

        // Assert
        Assert.Equal(saved, _details.TagEditor.Editing);
    }

    [Fact]
    public void RefreshingDuringATagResizeKeepsTheDraggedRange()
    {
        // Arrange
        var earlier = new TimeRange(Hour.Start.AddMinutes(-30), Hour.End);
        _selection.GrabEdge(anchor: Hour.End, Block(new TagBlock(Tag)));
        _selection.DragTo(earlier.Start);

        // Act
        _selection.Refresh([Lane(LaneKind.Tags, Block(new TagBlock(Tag)))]);

        // Assert
        Assert.Equal(earlier, _selection.Range);
        Assert.Equal(LaneKind.Tags, _selection.Lane);
    }

    [Fact]
    public void RefreshingAfterAResizeThatWasNotSavedShowsTheTagsRange()
    {
        // Arrange
        _selection.GrabEdge(anchor: Hour.Start, Block(new TagBlock(Tag)));
        _selection.DragTo(Hour.End.AddMinutes(30));
        _selection.EndDrag();

        // Act
        _selection.Refresh([Lane(LaneKind.Tags, Block(new TagBlock(Tag)))]);

        // Assert
        Assert.Equal(Hour, _selection.Range);
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

    [Fact]
    public void ExpectingATagTellsTheTimelineToRedrawTheOutline()
    {
        // Arrange
        _selection.SelectTime(Hour);
        var changes = 0;
        _selection.Changed += () => changes++;

        // Act
        _selection.ExpectTag();

        // Assert
        Assert.Equal(1, changes);
    }

    [Fact]
    public void DraggingSelectsTheTimeFromTheAnchor()
    {
        // Arrange
        _selection.StartDrag(anchor: Hour.End);

        // Act
        _selection.DragTo(Hour.Start);

        // Assert
        Assert.Equal(Hour, _selection.Range);
        Assert.Null(_selection.Lane);
        Assert.Equal("Selected time", _details.Heading);
    }

    [Fact]
    public void DraggingWithoutADragDoesNothing()
    {
        // Act
        _selection.DragTo(Hour.End);

        // Assert
        Assert.False(_selection.HasSelection);
    }

    [Fact]
    public void ANewDragThatSelectedNoTimeIsStillAClick()
    {
        // Arrange
        _selection.StartDrag(anchor: Hour.Start);

        // Act
        _selection.DragTo(Hour.Start);

        // Assert
        Assert.True(_selection.IsStillAClick);
    }

    [Fact]
    public void ANewDragThatSelectedTimeIsNotAClick()
    {
        // Arrange
        _selection.StartDrag(anchor: Hour.End);

        // Act
        _selection.DragTo(Hour.Start);

        // Assert
        Assert.False(_selection.IsStillAClick);
    }

    [Fact]
    public void ANewDragBackToTheAnchorIsNotAClick()
    {
        // Arrange
        _selection.StartDrag(anchor: Hour.End);
        _selection.DragTo(Hour.Start);

        // Act
        _selection.DragTo(Hour.End);

        // Assert
        Assert.False(_selection.IsStillAClick);
    }

    [Fact]
    public void DraggingAnEdgeIsNotAClick()
    {
        // Arrange
        _selection.SelectTime(Hour);

        // Act
        _selection.GrabEdge(anchor: Hour.Start, tagBlock: null);

        // Assert
        Assert.False(_selection.IsStillAClick);
    }

    [Fact]
    public void DraggingATagsEdgeResizesItWithoutTaggingNewTime()
    {
        // Arrange
        var longer = new TimeRange(Hour.Start, Hour.End.AddMinutes(30));
        _selection.GrabEdge(anchor: Hour.Start, Block(new TagBlock(Tag)));

        // Act
        _selection.DragTo(longer.End);

        // Assert
        Assert.Equal(longer, _selection.Range);
        Assert.Equal(LaneKind.Tags, _selection.Lane);
        Assert.Equal(Tag, _details.TagEditor.Editing);
    }

    [Fact]
    public void EndingATagResizeGivesTheTagAndItsNewRange()
    {
        // Arrange
        var longer = new TimeRange(Hour.Start, Hour.End.AddMinutes(30));
        _selection.GrabEdge(anchor: Hour.Start, Block(new TagBlock(Tag)));
        _selection.DragTo(longer.End);

        // Act
        var resize = _selection.EndDrag();

        // Assert
        Assert.Equal((Tag, longer), resize);
        Assert.False(_selection.IsDragging);
    }

    [Fact]
    public void EndingATagDragThatChangedNothingGivesNoResize()
    {
        // Arrange
        _selection.GrabEdge(anchor: Hour.Start, Block(new TagBlock(Tag)));

        // Act
        var resize = _selection.EndDrag();

        // Assert
        Assert.Null(resize);
    }

    [Fact]
    public void EndingAPlainDragGivesNoResize()
    {
        // Arrange
        _selection.StartDrag(anchor: Hour.Start);
        _selection.DragTo(Hour.End);

        // Act
        var resize = _selection.EndDrag();

        // Assert
        Assert.Null(resize);
    }

    [Fact]
    public void EndingADragStopsDragging()
    {
        // Arrange
        _selection.StartDrag(anchor: Hour.Start);

        // Act
        _selection.EndDrag();

        // Assert
        Assert.False(_selection.IsDragging);
    }

    [Fact]
    public void ShowingWhereADragWouldSnapTellsTheTimelineToRedraw()
    {
        // Arrange
        var changes = 0;
        _selection.Changed += () => changes++;

        // Act
        _selection.ShowSnapPreview(Hour.Start);

        // Assert
        Assert.Equal(Hour.Start, _selection.SnapPreviewUtc);
        Assert.Equal(1, changes);
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
