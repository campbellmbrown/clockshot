using System.IO.Abstractions.TestingHelpers;

using ClockShot.App.Services;
using ClockShot.Core.Models;

namespace ClockShot.App.Tests.Services;

public sealed class ScreenshotImageLoaderTests
{
    private static readonly Screenshot Screenshot = new(
        Id: 1,
        TakenUtc: new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc),
        ImagePath: "screenshots/1.webp",
        Width: 100,
        Height: 100);

    [Fact]
    public async Task AMissingFileGivesNoImage()
    {
        // Arrange
        var loader = new ScreenshotImageLoader(new FullPathScreenshotStore(), new MockFileSystem());

        // Act
        var image = await loader.LoadAsync(Screenshot, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(image);
    }

    [Fact]
    public async Task AFileThatCannotBeOpenedGivesNoImage()
    {
        // Arrange
        var files = new MockFileSystem();
        files.AddFile(Screenshot.ImagePath, new MockFileData([1, 2, 3]) { AllowedFileShare = FileShare.None });
        var loader = new ScreenshotImageLoader(new FullPathScreenshotStore(), files);

        // Act
        var image = await loader.LoadAsync(Screenshot, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(image);
    }
}
