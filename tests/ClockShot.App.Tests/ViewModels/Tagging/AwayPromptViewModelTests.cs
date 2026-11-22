using ClockShot.App.Services;
using ClockShot.App.ViewModels.Tagging;
using ClockShot.Core.Models;
using ClockShot.Core.Tagging;
using ClockShot.Data.Projects;
using ClockShot.Data.Tagging;

using NSubstitute;

namespace ClockShot.App.Tests.ViewModels.Tagging;

public sealed class AwayPromptViewModelTests
{
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Away = new(Nine.AddMinutes(45), Nine.AddMinutes(90));

    // Two hours ahead of UTC, so the message shows that the times are converted.
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone(
        id: "Test Time",
        baseUtcOffset: TimeSpan.FromHours(2),
        displayName: "Test time (UTC+2)",
        standardDisplayName: "Test Time");

    private readonly TagEditorViewModel _editor;

    public AwayPromptViewModelTests()
    {
        // These tests never load or save projects or tags.
        var database = UnopenedDatabase.Create();
        var entries = new TimeEntryRepository(database);
        _editor = new TagEditorViewModel(
            new ProjectRepository(database),
            entries,
            new Tagger(entries),
            Substitute.For<IDialogService>());
    }

    [Fact]
    public void SaysWhenTheUserWasAway()
    {
        // Act
        var prompt = new AwayPromptViewModel(_editor, Away, Zone);

        // Assert
        Assert.Equal("You were away 11:45 - 12:30 (45 min). What were you doing?", prompt.Message);
    }

    [Fact]
    public void GetsReadyToTagTheAwayTime()
    {
        // Act
        _ = new AwayPromptViewModel(_editor, Away, Zone);

        // Assert
        Assert.Equal(Away, _editor.Target);
        Assert.Null(_editor.Editing);
    }

    [Fact]
    public void OffersToSkipAndLeavesEnterToTheTagEditor()
    {
        // Act
        var prompt = new AwayPromptViewModel(_editor, Away, Zone);

        // Assert
        Assert.Equal("Skip, leave it untagged", prompt.CloseText);
        Assert.False(prompt.ClosesOnEnter);
    }
}
