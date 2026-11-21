using System.IO.Abstractions.TestingHelpers;

using ClockShot.App.Services;
using ClockShot.Core.Models;
using ClockShot.Data;
using ClockShot.Data.Screenshots;

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
        var files = new MockFileSystem();
        var loader = new ScreenshotImageLoader(Store(files), files);

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
        var store = Store(files);
        files.AddFile(
            store.FullPath(Screenshot.ImagePath),
            new MockFileData([1, 2, 3]) { AllowedFileShare = FileShare.None });
        var loader = new ScreenshotImageLoader(store, files);

        // Act
        var image = await loader.LoadAsync(Screenshot, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(image);
    }

    private static ScreenshotStore Store(MockFileSystem files) =>
        new(files, new DataPaths("data"), UnopenedDatabase.Create(), TimeProvider.System);
}
