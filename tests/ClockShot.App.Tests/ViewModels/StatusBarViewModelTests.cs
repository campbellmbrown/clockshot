using ClockShot.App.ViewModels;
using ClockShot.Core.Models;
using ClockShot.Core.Tracking;

namespace ClockShot.App.Tests.ViewModels;

public sealed class StatusBarViewModelTests
{
    private static readonly ForegroundWindow Editor = new(
        Handle: 1,
        ProcessName: "Editor.exe",
        ExecutablePath: null,
        DisplayName: "Editor",
        Title: "a.cs");

    [Theory]
    [InlineData(ComputerState.Active, StatusBarState.Tracking)]
    [InlineData(ComputerState.Away, StatusBarState.Away)]
    [InlineData(ComputerState.Locked, StatusBarState.Locked)]
    public void ShowsTheComputerState(ComputerState computerState, StatusBarState expected)
    {
        // Act
        var state = StatusBarViewModel.StateOf(new TrackingStatus(IsPaused: false, computerState, Editor));

        // Assert
        Assert.Equal(expected, state);
    }

    [Fact]
    public void ShowsPausedWhenPaused()
    {
        // Act
        var state = StatusBarViewModel.StateOf(new TrackingStatus(IsPaused: true, State: null, Window: null));

        // Assert
        Assert.Equal(StatusBarState.Paused, state);
    }

    [Fact]
    public void ShowsStartingBeforeTheFirstSample()
    {
        // Act
        var state = StatusBarViewModel.StateOf(TrackingStatus.NotStarted);

        // Assert
        Assert.Equal(StatusBarState.Starting, state);
    }

    [Fact]
    public void ShowsTheAppAndTitleOfTheRecordedWindow()
    {
        // Act
        var text = StatusBarViewModel.WindowTextOf(new TrackingStatus(IsPaused: false, ComputerState.Active, Editor));

        // Assert
        Assert.Equal("Editor - a.cs", text);
    }

    [Fact]
    public void ShowsNoWindowWhenNothingIsRecorded()
    {
        // Act
        var text = StatusBarViewModel.WindowTextOf(TrackingStatus.Paused);

        // Assert
        Assert.Empty(text);
    }
}
