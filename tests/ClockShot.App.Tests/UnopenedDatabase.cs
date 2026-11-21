using ClockShot.Data.Sqlite;

using Microsoft.Data.Sqlite;

namespace ClockShot.App.Tests;

/// <summary>
/// Gives a database to tests that build a class needing one but never use it.
/// </summary>
public static class UnopenedDatabase
{
    // The tests never connect, so the connection string can be empty.
    public static SqliteDatabase Create() => new(new SqliteConnectionStringBuilder());
}
