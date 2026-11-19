using ClockShot.Data.Sqlite;

using Dapper;

using Microsoft.Data.Sqlite;

namespace ClockShot.Data.Tests;

public sealed class SqliteDatabaseTests
{
    [Fact]
    public void MigrateCreatesEveryTable()
    {
        // Arrange
        using var data = new MemoryDatabase();
        using var connection = data.Database.Open();

        // Act
        var tables = connection.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name");

        // Assert
        Assert.Equal(
            [
                "activity_segment",
                "application",
                "client",
                "computer_state_segment",
                "project",
                "screenshot",
                "task",
                "time_entry",
            ],
            tables);
    }

    [Fact]
    public void MigrateAgainChangesNothing()
    {
        // Arrange
        using var data = new MemoryDatabase();
        using var connection = data.Database.Open();
        var version = connection.ExecuteScalar<long>("PRAGMA user_version;");

        // Act
        data.Database.Migrate();

        // Assert
        Assert.Equal(version, connection.ExecuteScalar<long>("PRAGMA user_version;"));
    }

    [Fact]
    public void EachMigrationHasItsOwnNumber()
    {
        // Act
        var versions = SqliteDatabase.Migrations().Select(migration => migration.Version).ToList();

        // Assert
        Assert.NotEmpty(versions);
        Assert.Distinct(versions);
    }

    [Fact]
    public void DatabaseUsesWriteAheadLogging()
    {
        // Arrange
        using var data = new TempDatabase();
        using var connection = data.Database.Open();

        // Act
        var mode = connection.ExecuteScalar<string>("PRAGMA journal_mode;");

        // Assert
        Assert.Equal("wal", mode);
    }

    [Fact]
    public void ApplicationsAreUniqueIgnoringCase()
    {
        // Arrange
        using var data = new MemoryDatabase();
        using var connection = data.Database.Open();
        const string Insert = """
            INSERT INTO application (process_name, exe_path, display_name, color)
            VALUES (@Name, @Path, 'Editor', '#000000')
            """;
        connection.Execute(Insert, new { Name = "Editor.exe", Path = @"C:\Editor.exe" });

        // Act
        var exception = Record.Exception(
            () => connection.Execute(Insert, new { Name = "EDITOR.EXE", Path = @"C:\EDITOR.EXE" }));

        // Assert
        Assert.IsType<SqliteException>(exception);
    }

    [Fact]
    public void ForeignKeysAreEnforced()
    {
        // Arrange
        using var data = new MemoryDatabase();
        using var connection = data.Database.Open();

        // Act
        var exception = Record.Exception(() => connection.Execute(
            "INSERT INTO activity_segment (start_utc, end_utc, application_id, window_title) VALUES (0, 0, 999, '')"));

        // Assert
        Assert.IsType<SqliteException>(exception);
    }
}
