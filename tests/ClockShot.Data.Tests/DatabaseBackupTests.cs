using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ClockShot.Data.Tests;

public sealed class DatabaseBackupTests
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.CreateCustomTimeZone(
        id: "Test Time",
        baseUtcOffset: TimeSpan.FromHours(2),
        displayName: "Test time (UTC+2)",
        standardDisplayName: "Test Time");

    [Fact]
    public async Task CopiesTheDatabaseIntoTheBackupsFolderNamedByLocalTime()
    {
        // Arrange
        using var data = new TempDatabase();
        using (var connection = data.Database.Open())
        {
            await connection.ExecuteAsync("INSERT INTO client (name) VALUES ('Client A')");
        }

        var time = new FakeTimeProvider(new DateTimeOffset(2026, 3, 5, 14, 30, 15, TimeSpan.Zero));
        time.SetLocalTimeZone(Zone);
        var paths = new DataPaths(data.Path);
        var backup = new DatabaseBackup(data.Database, paths, time);

        // Act
        var path = await backup.BackUpAsync();

        // Assert
        Assert.Equal(Path.Combine(paths.Backups, "clockshot-2026-03-05-163015.db"), path);
        var copySettings = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false };
        using var copy = new SqliteConnection(copySettings.ToString());
        Assert.Equal("Client A", await copy.ExecuteScalarAsync<string>("SELECT name FROM client"));
    }
}
