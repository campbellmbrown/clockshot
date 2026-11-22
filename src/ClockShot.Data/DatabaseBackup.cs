using System.Globalization;

using ClockShot.Core;
using ClockShot.Data.Sqlite;

namespace ClockShot.Data;

/// <summary>
/// Backs up the database into the backups folder, in a file named by local time.
/// </summary>
public sealed class DatabaseBackup(SqliteDatabase database, DataPaths paths, TimeProvider time)
{
    // Backups sort by name in time order, except when the clocks change.
    private const string TimeFormat = "yyyy-MM-dd-HHmmss";

    /// <returns>The backup file's path.</returns>
    /// <remarks>SQLite's backup blocks until it's done, so it runs off the caller's thread.</remarks>
    public Task<string> BackUpAsync() => Task.Run(() =>
    {
        Directory.CreateDirectory(paths.Backups);
        var fileName = $"{AppInfo.FilePrefix}-{time.GetLocalNow().ToString(TimeFormat, CultureInfo.InvariantCulture)}.db";
        var path = Path.Combine(paths.Backups, fileName);
        database.BackUpTo(path);
        return path;
    });
}
