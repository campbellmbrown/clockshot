using System.Globalization;
using System.IO.Abstractions;
using System.IO.Hashing;

using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;
using ClockShot.Data.Sqlite;

namespace ClockShot.Data.Screenshots;

/// <summary>
/// Saves screenshots as WebP files in day folders named by local time, and indexes them in the database.
/// Saving isn't thread-safe.
/// The tracking loop saves one screenshot at a time.
/// </summary>
/// <param name="time">Gives the local time zone for folder and file names.</param>
public sealed class ScreenshotStore(IFileSystem files, DataPaths paths, SqliteDatabase database, TimeProvider time)
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
}
