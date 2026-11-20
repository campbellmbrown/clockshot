using System.IO.Abstractions.TestingHelpers;

using ClockShot.App.Services;
using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Models;
using ClockShot.Core.Settings;
using ClockShot.Data;
using ClockShot.Data.Settings;

using Microsoft.Extensions.Logging.Abstractions;

namespace ClockShot.App.Tests.ViewModels.Timeline;

public sealed class ScreenshotPreviewViewModelTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly JsonSettingsStore _settings = new(
        new MockFileSystem(),
        new DataPaths("data"),
        NullLogger<JsonSettingsStore>.Instance);

    [Fact]
    public void ChangingTheModeSavesIt()
    {
        // Arrange
        using var preview = Preview();

        // Act
        preview.Mode = ScreenshotPreviewMode.BelowTimeline;

        // Assert
        Assert.Equal(ScreenshotPreviewMode.BelowTimeline, _settings.Current.Screenshots.PreviewMode);
        Assert.True(preview.IsBelowTimeline);
    }

    [Fact]
    public void CyclingFromTheLastModeWrapsRoundToTheFirst()
    {
        // Arrange
        using var preview = Preview();
        preview.Mode = ScreenshotPreviewMode.SeparateWindow;

        // Act
        preview.CycleModeCommand.Execute(null);

        // Assert
        Assert.Equal(ScreenshotPreviewMode.PopupByCursor, preview.Mode);
    }

    [Fact]
    public void PausingKeepsTheTimeLastShown()
    {
        // Arrange
        using var preview = Preview();
        preview.ShownAtUtc = NineAm;

        // Act
        preview.TogglePauseCommand.Execute(null);

        // Assert
        Assert.Equal(NineAm, preview.PausedAtUtc);
        Assert.True(preview.IsPaused);
        Assert.Equal("Resume the preview", preview.PauseText);
    }

    [Fact]
    public void TogglingAgainResumes()
    {
        // Arrange
        using var preview = Preview();
        preview.ShownAtUtc = NineAm;
        preview.TogglePauseCommand.Execute(null);

        // Act
        preview.TogglePauseCommand.Execute(null);

        // Assert
        Assert.Null(preview.PausedAtUtc);
        Assert.False(preview.IsPaused);
    }

    [Fact]
    public void PausingAndResumingTellTheTimelineToRedraw()
    {
        // Arrange
        using var preview = Preview();
        preview.ShownAtUtc = NineAm;
        var changes = 0;
        preview.PauseChanged += () => changes++;

        // Act
        preview.TogglePauseCommand.Execute(null);
        preview.Resume();

        // Assert
        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task AScreenshotThatCannotBeOpenedStillShowsItsDetails()
    {
        // Arrange
        using var preview = Preview();
        var missing = new Screenshot(
            Id: 1,
            TakenUtc: NineAm,
            ImagePath: "screenshots/missing.webp",
            Width: 100,
            Height: 100);

        // Act
        await preview.ShowAsync(missing, takenText: "09:00:00", applicationName: "Editor", windowTitle: "notes.txt");

        // Assert
        Assert.Equal("09:00:00", preview.TakenText);
        Assert.Equal("Editor", preview.ApplicationName);
        Assert.Equal("notes.txt", preview.WindowTitle);
        Assert.Equal("This screenshot couldn't be opened.", preview.Message);
    }

    private ScreenshotPreviewViewModel Preview() =>
        new(new ScreenshotImageLoader(new FullPathScreenshotStore(), new MockFileSystem()), _settings);
}
