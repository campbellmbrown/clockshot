using System.IO.Abstractions.TestingHelpers;

using ClockShot.App.Services;
using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Models;

namespace ClockShot.App.Tests.ViewModels.Timeline;

public sealed class ScreenshotPreviewViewModelTests
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

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

    private static ScreenshotPreviewViewModel Preview() =>
        new(new ScreenshotImageLoader(new FullPathScreenshotStore(), new MockFileSystem()));
}
