using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class BlockHeadingTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Hour = new(NineAm, NineAm.AddHours(1));

    private static readonly ProjectCatalog Catalog = new(
        [new Client(Id: 1, Name: "Client A", IsArchived: false)],
        [
            new Project(Id: 10, ClientId: 1, ParentId: null, Name: "Website", IsArchived: false),
            new Project(Id: 11, ClientId: null, ParentId: 10, Name: "Checkout", IsArchived: false),
            new Project(Id: 20, ClientId: null, ParentId: null, Name: "Internal", IsArchived: false),
        ],
        [new ProjectTask(Id: 100, ProjectId: 11, Name: "Login page", IsArchived: false)]);

    [Fact]
    public void ATagShowsItsLabelThenItsClientParentsAndNote()
    {
        // Arrange
        var entry = new TimeEntry(Id: 1, Hour, ProjectId: 11, TaskId: 100, Note: "Fixing the form");

        // Act
        var heading = BlockHeading.For(Block(new TagBlock(entry)), Catalog);

        // Assert
        var expected = new BlockHeading(
            Heading: "Checkout > Login page",
            Subheading: "Client A > Website - Fixing the form");
        Assert.Equal(expected, heading);
    }

    [Fact]
    public void ATagWithNoClientParentsOrNoteHasNoSubheading()
    {
        // Arrange
        var entry = new TimeEntry(Id: 1, Hour, ProjectId: 20, TaskId: null, Note: null);

        // Act
        var heading = BlockHeading.For(Block(new TagBlock(entry)), Catalog);

        // Assert
        Assert.Equal(new BlockHeading(Heading: "Internal", Subheading: ""), heading);
    }

    [Fact]
    public void AWindowShowsItsApplicationThenItsTitle()
    {
        // Arrange
        var editor = new TrackedApplication(
            Id: 1,
            ProcessName: "Editor.exe",
            ExecutablePath: null,
            DisplayName: "Editor",
            ColorHex: "#336699");

        // Act
        var heading = BlockHeading.For(Block(new WindowBlock(editor, Title: "notes.txt")), Catalog);

        // Assert
        Assert.Equal(new BlockHeading(Heading: "Editor", Subheading: "notes.txt"), heading);
    }

    [Fact]
    public void AComputerStateShowsItsName()
    {
        // Act
        var heading = BlockHeading.For(Block(new ComputerStateBlock(ComputerState.Away)), Catalog);

        // Assert
        Assert.Equal(new BlockHeading(Heading: "Away", Subheading: ""), heading);
    }

    private static TimelineBlock Block(object payload) =>
        new(Hour, Label: "label", new HexColor("#336699"), payload);
}
