using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class BlockHeadingTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Hour = new(NineAm, NineAm.AddHours(1));

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
        var heading = BlockHeading.For(Block(new WindowBlock(editor, Title: "notes.txt")));

        // Assert
        Assert.Equal(new BlockHeading(Heading: "Editor", Subheading: "notes.txt"), heading);
    }

    [Fact]
    public void AComputerStateShowsItsName()
    {
        // Act
        var heading = BlockHeading.For(Block(new ComputerStateBlock(ComputerState.Away)));

        // Assert
        Assert.Equal(new BlockHeading(Heading: "Away", Subheading: ""), heading);
    }

    private static TimelineBlock Block(object payload) =>
        new(Hour, Label: "label", new HexColor("#336699"), payload);
}
