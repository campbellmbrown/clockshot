using System.Globalization;
using System.IO.Abstractions;
using System.IO.Hashing;

using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;
using ClockShot.Data.Sqlite;

using Microsoft.Data.Sqlite;

namespace ClockShot.Data.Screenshots;

/// <summary>
/// Saves screenshots as WebP files in day folders named by local time, and indexes them in the database.
/// Saving isn't thread-safe.
/// The tracking loop saves one screenshot at a time.
/// Deleting can run while a screenshot is saving, because they share no fields and each opens its own connection.
/// </summary>
/// <param name="time">Gives the local time zone for folder and file names.</param>
public sealed class ScreenshotStore(IFileSystem files, DataPaths paths, SqliteDatabase database, TimeProvider time)
    : IScreenshotStore
{
    private const string FileExtension = ".webp";
    private const string TemporaryExtension = ".tmp";
    private const string RelativePathFormat = "yyyy'/'MM'/'dd'/'HHmmss'_'fff";

    private ulong? _previousHash;

    public async Task<Screenshot?> SaveAsync(
        CapturedImage image,
        DateTime takenUtc,
        ScreenshotEncoding encoding,
        CancellationToken cancellationToken = default)
    {
        var hash = XxHash3.HashToUInt64(image.Pixels);
        if (hash == _previousHash)
        {
            return null;
        }

        var encoded = WebpEncoder.Encode(image, encoding);
        var relativePath = RelativePathFor(takenUtc, time.LocalTimeZone);
        var fullPath = FullPath(relativePath);
        await WriteNewFileAsync(fullPath, encoded.Webp, cancellationToken).ConfigureAwait(false);

        try
        {
            using var connection = database.Open();
            var id = ScreenshotTable.Insert(connection, takenUtc, relativePath, encoded.Width, encoded.Height);

            // Remembered only once saved, so a failed save doesn't make the next identical capture look like a duplicate.
            _previousHash = hash;
            return new Screenshot(id, takenUtc, relativePath, encoded.Width, encoded.Height);
        }
        catch
        {
            // Cleanup finds screenshots through the index, so a file without a row would never be deleted.
            files.File.Delete(fullPath);
            throw;
        }
    }

    // Microsoft.Data.Sqlite and file deletes are synchronous.
    // Running them on another thread keeps the caller responsive.
    public Task<int> DeleteTakenBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        Task.Run(() => DeleteTakenBefore(cutoffUtc, cancellationToken), cancellationToken);

    public Task<StorageUse> GetStorageUseAsync() => Task.Run(() =>
    {
        if (!files.Directory.Exists(paths.Screenshots))
        {
            return new StorageUse(Bytes: 0, Files: 0);
        }

        var screenshots = files.DirectoryInfo.New(paths.Screenshots)
            .EnumerateFiles("*" + FileExtension, SearchOption.AllDirectories)
            .ToList();
        return new StorageUse(screenshots.Sum(f => f.Length), screenshots.Count);
    });

    public string FullPath(string imagePath) =>
        files.Path.Combine(paths.Screenshots, imagePath.Replace('/', files.Path.DirectorySeparatorChar));

    /// <summary>
    /// The stored path, such as "2026/11/10/113005_120.webp".
    /// The folders and file name use <paramref name="zone"/>'s local time.
    /// A '/' separates them on every platform.
    /// </summary>
    internal static string RelativePathFor(DateTime takenUtc, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeFromUtc(takenUtc, zone).ToString(RelativePathFormat, CultureInfo.InvariantCulture)
        + FileExtension;

    /// <summary>
    /// Writes the file under a temporary name first, so a crash never leaves a half-written image under the real name.
    /// </summary>
    /// <exception cref="IOException">A file with this name already exists.</exception>
    private async Task WriteNewFileAsync(string fullPath, byte[] contents, CancellationToken cancellationToken)
    {
        files.Directory.CreateDirectory(files.Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + TemporaryExtension;
        await files.File.WriteAllBytesAsync(temporaryPath, contents, cancellationToken).ConfigureAwait(false);

        try
        {
            // A name repeats when the clocks go back, and replacing the earlier file would leave two rows sharing one.
            files.File.Move(temporaryPath, fullPath, overwrite: false);
        }
        catch
        {
            files.File.Delete(temporaryPath);
            throw;
        }
    }

    private int DeleteTakenBefore(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        var failures = new List<Exception>();
        var leftoverFolders = DeleteLeftoverTemporaryFiles(cutoffUtc, failures);

        using var connection = database.Open();
        var deleted = new List<Screenshot>();
        foreach (var screenshot in ScreenshotTable.GetTakenBefore(connection, cutoffUtc))
        {
            // Stopping here leaves rows whose files are gone. The next run deletes them and their folders.
            cancellationToken.ThrowIfCancellationRequested();
            if (TryDelete(FullPath(screenshot.ImagePath), failures))
            {
                deleted.Add(screenshot);
            }
        }

        DeleteRows(connection, deleted);
        var deletedFolders = deleted.Select(s => files.Path.GetDirectoryName(FullPath(s.ImagePath))!);
        DeleteEmptyFolders(deletedFolders.Concat(leftoverFolders).Distinct());
        if (failures.Count > 0)
        {
            throw new AggregateException(
                $"Old screenshots deleted: {deleted.Count}. Files that couldn't be deleted: {failures.Count}.",
                failures);
        }

        return deleted.Count;
    }

    /// <summary>
    /// Deletes a file, or records why it can't, such as when a viewer has it open.
    /// A file that's already gone counts as deleted, even when its folder is gone too.
    /// </summary>
    private bool TryDelete(string path, List<Exception> failures)
    {
        try
        {
            files.File.Delete(path);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            // Someone deleted the folder by hand, which took the file with it.
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(ex);
            return false;
        }
    }

    private static void DeleteRows(SqliteConnection connection, List<Screenshot> screenshots)
    {
        if (screenshots.Count == 0)
        {
            return;
        }

        using var transaction = connection.BeginTransaction();
        ScreenshotTable.Delete(connection, screenshots.Select(s => s.Id), transaction);
        transaction.Commit();
    }

    /// <summary>
    /// Deletes temporary files that a crash left between writing a screenshot and renaming it.
    /// A file written before the cutoff can't still be in use, because the cutoff is at least a day ago.
    /// </summary>
    /// <returns>The folders of the files that were deleted.</returns>
    private List<string> DeleteLeftoverTemporaryFiles(DateTime cutoffUtc, List<Exception> failures)
    {
        if (!files.Directory.Exists(paths.Screenshots))
        {
            return [];
        }

        var leftovers = files.DirectoryInfo.New(paths.Screenshots)
            .EnumerateFiles("*" + TemporaryExtension, SearchOption.AllDirectories)
            .Where(file => file.LastWriteTimeUtc < cutoffUtc)
            .ToList();
        return [.. leftovers.Where(file => TryDelete(file.FullName, failures)).Select(file => file.DirectoryName!)];
    }

    /// <summary>
    /// Removes day, month and year folders left empty after a cleanup.
    /// </summary>
    private void DeleteEmptyFolders(IEnumerable<string> dayFolders)
    {
        var root = files.Path.GetFullPath(paths.Screenshots);
        foreach (var dayFolder in dayFolders)
        {
            var folder = dayFolder;
            while (folder is not null
                && files.Path.GetFullPath(folder) != root
                && files.Directory.Exists(folder)
                && !files.Directory.EnumerateFileSystemEntries(folder).Any())
            {
                files.Directory.Delete(folder);
                folder = files.Path.GetDirectoryName(folder);
            }
        }
    }
}
