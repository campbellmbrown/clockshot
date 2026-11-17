using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class TimelineLanesTests
{
    // The lanes never read segment ids, so every record shares one.
    private const long AnyId = 0;

    private const long EditorId = 1;

    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange FirstHour = new(Nine, Nine.AddHours(1));

    private static readonly TrackedApplication Editor =
        new(EditorId, "Editor.exe", @"C:\Editor.exe", "Editor", "#3E8E5E");

    [Fact]
    public void TheLanesAreComputerThenApplications()
    {
        // Act
        var lanes = TimelineLanes.Build(ActivityRecords.Empty);

        // Assert
        Assert.Equal([LaneKind.Computer, LaneKind.Applications], lanes.Select(lane => lane.Kind));
    }

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

    [Fact]
    public void EachWindowBecomesABlockInItsApplicationsColour()
    {
        // Arrange
        var records = ActivityRecords.Empty with
        {
            Activities = [new(AnyId, FirstHour, EditorId, WindowTitle: "notes.txt")],
            Applications = new Dictionary<long, TrackedApplication> { [EditorId] = Editor },
        };

        // Act
        var lane = Lane(records, LaneKind.Applications);

        // Assert
        var block = Assert.Single(lane.Blocks);
        var expected = new TimelineBlock(
            FirstHour,
            Label: "Editor - notes.txt",
            new HexColor("#3E8E5E"),
            Payload: new WindowBlock(Editor, Title: "notes.txt"));
        Assert.Equal(expected, block);
    }

    [Fact]
    public void AWindowOfAnUnknownApplicationIsLeftOut()
    {
        // Arrange
        var records = ActivityRecords.Empty with
        {
            Activities = [new(AnyId, FirstHour, EditorId, WindowTitle: "notes.txt")],
        };

        // Act
        var blocks = Lane(records, LaneKind.Applications).Blocks;

        // Assert
        Assert.Empty(blocks);
    }

    private static TimelineLane Lane(ActivityRecords records, LaneKind kind) =>
        TimelineLanes.Build(records).Single(lane => lane.Kind == kind);
}
