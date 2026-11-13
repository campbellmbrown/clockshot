using ClockShot.Core;
using ClockShot.Data.Sqlite;

using Microsoft.Data.Sqlite;

namespace ClockShot.Data.Tests;

/// <summary>
/// A migrated database in a fresh folder for one test, deleted afterwards.
/// It's for tests that need real files.
/// Other tests use <see cref="MemoryDatabase"/>, which is faster.
/// </summary>
public sealed class TempDatabase : IDisposable
{
    public TempDatabase()
    {
        Database = new SqliteDatabase(new DataPaths(Path).Database);
        Database.Migrate();
    }

    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{AppInfo.FilePrefix}-tests", Guid.NewGuid().ToString("N"));

    public SqliteDatabase Database { get; }

    public void Dispose()
    {
        // Pooled connections keep the database file open, so release them before the folder is deleted.
        SqliteConnection.ClearAllPools();
        Directory.Delete(Path, recursive: true);
    }
}
