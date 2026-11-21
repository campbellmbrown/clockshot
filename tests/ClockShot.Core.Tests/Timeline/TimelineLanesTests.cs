using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class TimelineLanesTests
{
    // The lanes never read segment or entry ids, so every record shares one.
    private const long AnyId = 0;

    private const long EditorId = 1;
    private const long WebsiteId = 10;
    private const long LoginPageId = 20;

    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange FirstHour = new(Nine, Nine.AddHours(1));

    private static readonly TrackedApplication Editor =
        new(EditorId, "Editor.exe", @"C:\Editor.exe", "Editor", "#3E8E5E");

    [Fact]
    public void TheLanesAreTagsThenComputerThenApplications()
    {
        // Act
        var lanes = TimelineLanes.Build(ActivityRecords.Empty, tags: [], ProjectCatalog.Empty);

        // Assert
        Assert.Equal([LaneKind.Tags, LaneKind.Computer, LaneKind.Applications], lanes.Select(lane => lane.Kind));
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

    [Fact]
    public void EachTagBecomesABlockWithItsProjectsLabelAndColour()
    {
        // Arrange
        var catalog = new ProjectCatalog(
            clients: [],
            projects: [new Project(WebsiteId, ClientId: null, ParentId: null, "Website", IsArchived: false)],
            tasks: [new ProjectTask(LoginPageId, WebsiteId, "Login page", IsArchived: false)]);
        var tag = new TimeEntry(AnyId, FirstHour, WebsiteId, LoginPageId, Note: null);

        // Act
        var lanes = TimelineLanes.Build(ActivityRecords.Empty, [tag], catalog);

        // Assert
        var block = Assert.Single(lanes.Single(lane => lane.Kind == LaneKind.Tags).Blocks);
        var expected = new TimelineBlock(
            FirstHour,
            "Website > Login page",
            new HexColor(NameColor.For("Website > Login page")),
            Payload: new TagBlock(tag));
        Assert.Equal(expected, block);
    }

    private static TimelineLane Lane(ActivityRecords records, LaneKind kind) =>
        TimelineLanes.Build(records, tags: [], ProjectCatalog.Empty).Single(lane => lane.Kind == kind);
}
