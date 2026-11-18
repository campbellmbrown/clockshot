using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class HoverPreviewTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private static readonly TrackedApplication Editor = new(
        Id: 1,
        ProcessName: "Editor.exe",
        ExecutablePath: null,
        DisplayName: "Editor",
        ColorHex: "#336699");

    private static readonly WindowBlock NotesWindow = new(Editor, Title: "notes.txt");
    private static readonly WindowBlock TodoWindow = new(Editor, Title: "todo.txt");

    private static readonly Screenshot NotesScreenshot = new(
        Id: 1,
        TakenUtc: At(minutes: 10),
        ImagePath: "1.webp",
        Width: 100,
        Height: 100);

    private static readonly TimelineLane[] Lanes =
    [
        new(LaneKind.Computer, title: "Computer",
        [
            StateBlock(fromMinutes: 0, toMinutes: 60, ComputerState.Active),
            StateBlock(fromMinutes: 60, toMinutes: 90, ComputerState.Away),
            StateBlock(fromMinutes: 90, toMinutes: 120, ComputerState.Locked),
        ]),
        new(LaneKind.Applications, title: "Applications",
        [
            ApplicationBlock(fromMinutes: 0, toMinutes: 30, NotesWindow),
            ApplicationBlock(fromMinutes: 40, toMinutes: 60, TodoWindow),
        ]),
    ];

    private static readonly ScreenshotIndex Screenshots = new([NotesScreenshot]);

    [Fact]
    public void ShowsTheScreenshotOfTheWindowUnderThePointer()
    {
        // Act
        var preview = HoverPreview.At(Lanes, Screenshots, At(minutes: 20));

        // Assert
        Assert.Equal(new HoverPreview(HoverPreviewKind.Screenshot, NotesWindow, NotesScreenshot), preview);
    }

    [Fact]
    public void SaysWhenTheWindowUnderThePointerHasNoScreenshot()
    {
        // Act
        var preview = HoverPreview.At(Lanes, Screenshots, At(minutes: 50));

        // Assert
        Assert.Equal(new HoverPreview(HoverPreviewKind.NoScreenshotOfWindow, TodoWindow, Screenshot: null), preview);
    }

    [Fact]
    public void SaysWhenTheComputerWasAway()
    {
        // Act
        var preview = HoverPreview.At(Lanes, Screenshots, At(minutes: 75));

        // Assert
        Assert.Equal(HoverPreviewKind.Away, preview.Kind);
    }

    [Fact]
    public void SaysWhenTheComputerWasLocked()
    {
        // Act
        var preview = HoverPreview.At(Lanes, Screenshots, At(minutes: 105));

        // Assert
        Assert.Equal(HoverPreviewKind.Locked, preview.Kind);
    }

    [Fact]
    public void SaysNothingWasRecordedBetweenWindows()
    {
        // Act
        var preview = HoverPreview.At(Lanes, Screenshots, At(minutes: 35));

        // Assert
        Assert.Equal(HoverPreviewKind.NothingRecorded, preview.Kind);
    }

    [Fact]
    public void SaysNothingWasRecordedOutsideTheRecordedTime()
    {
        // Act
        var preview = HoverPreview.At(Lanes, Screenshots, At(minutes: 150));

        // Assert
        Assert.Equal(HoverPreviewKind.NothingRecorded, preview.Kind);
    }

    private static DateTime At(int minutes) => NineAm.AddMinutes(minutes);

    private static TimelineBlock StateBlock(int fromMinutes, int toMinutes, ComputerState state) =>
        new(
            new TimeRange(At(fromMinutes), At(toMinutes)),
            state.ToString(),
            new StateColor(state),
            new ComputerStateBlock(state));

    private static TimelineBlock ApplicationBlock(int fromMinutes, int toMinutes, WindowBlock window) =>
        new(new TimeRange(At(fromMinutes), At(toMinutes)), window.Title, new HexColor(Editor.ColorHex), window);
}
