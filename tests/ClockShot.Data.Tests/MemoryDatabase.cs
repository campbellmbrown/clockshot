using ClockShot.Data.Sqlite;

using Microsoft.Data.Sqlite;

namespace ClockShot.Data.Tests;

/// <summary>
/// A migrated database in memory for one test, gone once the test disposes it.
/// </summary>
public sealed class MemoryDatabase : IDisposable
{
    // SQLite deletes an in-memory database when its last connection closes.
    private readonly SqliteConnection _keepAlive;

    public MemoryDatabase()
    {
        Database = new SqliteDatabase(new SqliteConnectionStringBuilder
        {
            // A unique name keeps tests that run at the same time apart.
            DataSource = Guid.NewGuid().ToString("N"),
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        });
        _keepAlive = Database.Open();
        Database.Migrate();
    }

    public SqliteDatabase Database { get; }

    public void Dispose()
    {
        // Pooled connections stay open, which would keep the database in memory.
        SqliteConnection.ClearPool(_keepAlive);
        _keepAlive.Dispose();
    }
}
