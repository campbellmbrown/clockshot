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
        Assert.Equal("2026/11/10/113005_120.webp", saved!.ImagePath);
        Assert.True(_files.File.Exists(_store.FullPath(saved.ImagePath)));
    }

    [Fact]
    public async Task LeavesOnlyTheImageInTheDayFolder()
    {
        // Act
        var saved = await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);

        // Assert
        var dayFolder = _files.Path.GetDirectoryName(_store.FullPath(saved!.ImagePath))!;
        Assert.Equal(["113005_120.webp"], _files.Directory.EnumerateFiles(dayFolder).Select(_files.Path.GetFileName));
    }

    [Fact]
    public async Task IndexesTheScaledDownSize()
    {
        // Act
        var saved = await _store.SaveAsync(Image(blue: 10), Taken, Encoding with { MaxWidthPixels = 50 }, Cancellation);

        // Assert
        Assert.Equal((50, 50), (saved!.Width, saved.Height));
        using var connection = _data.Database.Open();
        var aroundTaken = new TimeRange(Taken.AddMinutes(-1), Taken.AddMinutes(1));
        Assert.Equal(saved, Assert.Single(ScreenshotTable.GetTakenIn(connection, aroundTaken)));
    }

    [Fact]
    public async Task RefusesToReplaceAScreenshotWithTheSameName()
    {
        // Arrange
        var first = await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);
        var firstBytes = await _files.File.ReadAllBytesAsync(_store.FullPath(first!.ImagePath), Cancellation);

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

    [Fact]
    public async Task SkipsAnImageIdenticalToThePreviousOne()
    {
        // Arrange
        await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);

        // Act
        var saved = await _store.SaveAsync(Image(blue: 10), Taken.AddSeconds(60), Encoding, Cancellation);

        // Assert
        Assert.Null(saved);
        Assert.Single(_files.Directory.EnumerateFiles(Paths.Screenshots, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SavesAnImageThatDiffersFromThePreviousOne()
    {
        // Arrange
        await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);

        // Act
        var saved = await _store.SaveAsync(Image(blue: 99), Taken.AddSeconds(60), Encoding, Cancellation);

        // Assert
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task SavesAnImageWhoseEarlierSaveFailed()
    {
        // Arrange
        await _store.SaveAsync(Image(blue: 10), Taken, Encoding, Cancellation);
        await Record.ExceptionAsync(() => _store.SaveAsync(Image(blue: 99), Taken, Encoding, Cancellation));

        // Act
        var saved = await _store.SaveAsync(Image(blue: 99), Taken.AddSeconds(60), Encoding, Cancellation);

        // Assert
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task DeletesOldScreenshotsAndTheFoldersTheyLeaveEmpty()
    {
        // Arrange
        await _store.SaveAsync(Image(blue: 1), Taken.AddDays(-40), Encoding, Cancellation);

        // Act
        var deleted = await _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation);

        // Assert
        Assert.Equal(1, deleted);
        Assert.Empty(_files.Directory.EnumerateFileSystemEntries(Paths.Screenshots));
    }

    [Fact]
    public async Task KeepsAMonthFolderThatStillHasScreenshots()
    {
        // Arrange
        var old = await _store.SaveAsync(Image(blue: 1), Taken.AddDays(-9), Encoding, Cancellation);
        await _store.SaveAsync(Image(blue: 2), Taken, Encoding, Cancellation);
        var oldDayFolder = _files.Path.GetDirectoryName(_store.FullPath(old!.ImagePath))!;

        // Act
        await _store.DeleteTakenBeforeAsync(Taken.AddDays(-5), Cancellation);

        // Assert
        Assert.False(_files.Directory.Exists(oldDayFolder));
        Assert.True(_files.Directory.Exists(_files.Path.GetDirectoryName(oldDayFolder)));
    }

    [Fact]
    public async Task KeepsRecentScreenshotsWhenDeletingOldOnes()
    {
        // Arrange
        await _store.SaveAsync(Image(blue: 1), Taken.AddDays(-40), Encoding, Cancellation);
        var recent = await _store.SaveAsync(Image(blue: 2), Taken, Encoding, Cancellation);

        // Act
        await _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation);

        // Assert
        Assert.True(_files.File.Exists(_store.FullPath(recent!.ImagePath)));
    }

    [Fact]
    public async Task DeletesTheRowsOfOldScreenshots()
    {
        // Arrange
        await _store.SaveAsync(Image(blue: 1), Taken.AddDays(-40), Encoding, Cancellation);

        // Act
        await _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation);

        // Assert
        using var connection = _data.Database.Open();
        Assert.Empty(ScreenshotTable.GetTakenBefore(connection, Taken));
    }

    [Fact]
    public async Task DeletesTheRowAndFoldersOfAScreenshotWhoseFileIsAlreadyGone()
    {
        // Arrange
        var gone = await _store.SaveAsync(Image(blue: 1), Taken.AddDays(-40), Encoding, Cancellation);
        _files.File.Delete(_store.FullPath(gone!.ImagePath));

        // Act
        var deleted = await _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation);

        // Assert
        Assert.Equal(1, deleted);
        Assert.Empty(_files.Directory.EnumerateFileSystemEntries(Paths.Screenshots));
        using var connection = _data.Database.Open();
        Assert.Empty(ScreenshotTable.GetTakenBefore(connection, Taken));
    }

    [Fact]
    public async Task DeletesTheRowOfAScreenshotWhoseFolderWasDeletedByHand()
    {
        // Arrange
        var gone = await _store.SaveAsync(Image(blue: 1), Taken.AddDays(-40), Encoding, Cancellation);
        _files.Directory.Delete(_files.Path.GetDirectoryName(_store.FullPath(gone!.ImagePath))!, recursive: true);

        // Act
        var deleted = await _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation);

        // Assert
        Assert.Equal(1, deleted);
        using var connection = _data.Database.Open();
        Assert.Empty(ScreenshotTable.GetTakenBefore(connection, Taken));
    }

    [Fact]
    public async Task SkipsAFileItCannotDeleteAndDeletesTheRest()
    {
        // Arrange
        var stuck = await _store.SaveAsync(Image(blue: 1), Taken.AddDays(-40), Encoding, Cancellation);
        var other = await _store.SaveAsync(Image(blue: 2), Taken.AddDays(-39), Encoding, Cancellation);
        _files.File.SetAttributes(_store.FullPath(stuck!.ImagePath), FileAttributes.ReadOnly);

        // Act
        var thrown = await Record.ExceptionAsync(() => _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation));

        // Assert
        Assert.IsType<AggregateException>(thrown);
        Assert.False(_files.File.Exists(_store.FullPath(other!.ImagePath)));
        using var connection = _data.Database.Open();
        Assert.Equal([stuck], ScreenshotTable.GetTakenBefore(connection, Taken));
    }

    [Fact]
    public async Task DeletesALeftoverTemporaryFileOlderThanTheCutoff()
    {
        // Arrange
        var leftover = LeftoverTemporaryFile(writtenUtc: Taken.AddDays(-40));

        // Act
        await _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation);

        // Assert
        Assert.False(_files.File.Exists(leftover));
        Assert.Empty(_files.Directory.EnumerateFileSystemEntries(Paths.Screenshots));
    }

    [Fact]
    public async Task ATemporaryFileItCannotDeleteDoesNotStopTheCleanup()
    {
        // Arrange
        var stuck = LeftoverTemporaryFile(writtenUtc: Taken.AddDays(-40));
        _files.File.SetAttributes(stuck, FileAttributes.ReadOnly);
        var old = await _store.SaveAsync(Image(blue: 1), Taken.AddDays(-40), Encoding, Cancellation);

        // Act
        var thrown = await Record.ExceptionAsync(() => _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation));

        // Assert
        Assert.IsType<AggregateException>(thrown);
        Assert.False(_files.File.Exists(_store.FullPath(old!.ImagePath)));
    }

    [Fact]
    public async Task KeepsATemporaryFileThatMayStillBeWritten()
    {
        // Arrange
        var recent = LeftoverTemporaryFile(writtenUtc: Taken);

        // Act
        await _store.DeleteTakenBeforeAsync(Taken.AddDays(-30), Cancellation);

        // Assert
        Assert.True(_files.File.Exists(recent));
    }

    /// <summary>
    /// A temporary file like one a crash leaves between writing a screenshot and renaming it.
    /// </summary>
    private string LeftoverTemporaryFile(DateTime writtenUtc)
    {
        var path = _store.FullPath("2026/10/01/090000_000.webp.tmp");
        _files.Directory.CreateDirectory(_files.Path.GetDirectoryName(path)!);
        _files.File.WriteAllBytes(path, [1]);
        _files.File.SetLastWriteTimeUtc(path, writtenUtc);
        return path;
    }

    private static CapturedImage Image(byte blue) => TestImages.Gradient(width: 100, height: 100, blue);
}
