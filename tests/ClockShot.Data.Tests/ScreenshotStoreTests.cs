using System.IO.Abstractions.TestingHelpers;

using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;
using ClockShot.Data.Screenshots;
using ClockShot.Data.Sqlite;

using Dapper;

using Microsoft.Extensions.Time.Testing;

namespace ClockShot.Data.Tests;

public sealed class ScreenshotStoreTests : IDisposable
{
    private static readonly DateTime Taken = new(2026, 11, 10, 9, 30, 5, 120, DateTimeKind.Utc);
    private static readonly ScreenshotEncoding Encoding = new(MaxWidthPixels: 1920, Quality: 70);
    private static readonly DataPaths Paths = new("data");
    private static readonly TimeZoneInfo TwoHoursAhead = TimeZoneInfo.CreateCustomTimeZone(
        id: "Test Plus Two",
        baseUtcOffset: TimeSpan.FromHours(2),
        displayName: "Test time (UTC+2)",
        standardDisplayName: "Test Plus Two");

    private readonly MockFileSystem _files = new();
    private readonly MemoryDatabase _data = new();
    private readonly ScreenshotStore _store;

    public ScreenshotStoreTests()
    {
        var time = new FakeTimeProvider();
        time.SetLocalTimeZone(TwoHoursAhead);
        _store = new ScreenshotStore(_files, Paths, _data.Database, time);
    }

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public void Dispose() => _data.Dispose();

    [Fact]
    public async Task SavesTheFileInFoldersForTheLocalDate()
    {
        // Act
        var saved = await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);

        // Assert
        Assert.Equal("2026/11/10/113005_120.webp", saved.ImagePath);
        Assert.True(_files.File.Exists(_store.FullPath(saved.ImagePath)));
    }

    [Fact]
    public async Task LeavesOnlyTheImageInTheDayFolder()
    {
        // Act
        var saved = await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);

        // Assert
        var dayFolder = _files.Path.GetDirectoryName(_store.FullPath(saved.ImagePath))!;
        Assert.Equal(["113005_120.webp"], _files.Directory.EnumerateFiles(dayFolder).Select(_files.Path.GetFileName));
    }

    [Fact]
    public async Task IndexesTheScaledDownSize()
    {
        // Act
        var saved = await _store.SaveAsync(Image(blue: 10), Taken, Encoding with { MaxWidthPixels = 50 }, Cancellation);

        // Assert
        Assert.Equal((50, 50), (saved.Width, saved.Height));
        using var connection = _data.Database.Open();
        var aroundTaken = new TimeRange(Taken.AddMinutes(-1), Taken.AddMinutes(1));
        Assert.Equal(saved, Assert.Single(ScreenshotTable.GetTakenIn(connection, aroundTaken)));
    }

    [Fact]
    public async Task RefusesToReplaceAScreenshotWithTheSameName()
    {
        // Arrange
        var first = await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);
        var firstBytes = await _files.File.ReadAllBytesAsync(_store.FullPath(first.ImagePath), Cancellation);

        // Act
        var thrown = await Record.ExceptionAsync(() => _store.SaveAsync(Image(blue: 99), Taken, Encoding, Cancellation));

        // Assert
        Assert.IsType<IOException>(thrown);
        var dayFolder = _files.Path.GetDirectoryName(_store.FullPath(first.ImagePath))!;
        Assert.Single(_files.Directory.EnumerateFiles(dayFolder));
        Assert.Equal(firstBytes, await _files.File.ReadAllBytesAsync(_store.FullPath(first.ImagePath), Cancellation));
    }

    [Fact]
    public async Task DeletesTheFileWhenIndexingFails()
    {
        // Arrange
        await using (var connection = _data.Database.Open())
        {
            await connection.ExecuteAsync("DROP TABLE screenshot");
        }

        // Act
        var thrown = await Record.ExceptionAsync(() => _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation));

        // Assert
        Assert.NotNull(thrown);
        Assert.Empty(_files.Directory.EnumerateFiles(Paths.Screenshots, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task IndexesTheSavedScreenshot()
    {
        // Act
        var saved = await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);

        // Assert
        using var connection = _data.Database.Open();
        var aroundTaken = new TimeRange(Taken.AddMinutes(-1), Taken.AddMinutes(1));
        Assert.Equal(saved, Assert.Single(ScreenshotTable.GetTakenIn(connection, aroundTaken)));
    }

    private static CapturedImage Image(byte blue) => TestImages.Gradient(width: 100, height: 100, blue);
}
