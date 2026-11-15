using System.Data;

using ClockShot.Core.Models;

using Dapper;

namespace ClockShot.Data.Sqlite;

/// <summary>
/// SQL for the screenshot index.
/// </summary>
internal static class ScreenshotTable
{
    private const string SelectScreenshots = "SELECT id, taken_utc, image_path, width, height FROM screenshot";

    public static long Insert(IDbConnection connection, DateTime takenUtc, string imagePath, int width, int height) =>
        connection.ExecuteScalar<long>(
            """
            INSERT INTO screenshot (taken_utc, image_path, width, height) VALUES (@Taken, @ImagePath, @Width, @Height);
            SELECT last_insert_rowid();
            """,
            new { Taken = UnixTime.ToMilliseconds(takenUtc), ImagePath = imagePath, Width = width, Height = height });

    public static IReadOnlyList<Screenshot> GetTakenIn(IDbConnection connection, TimeRange range) =>
        Query(
            connection,
            SelectScreenshots + " WHERE taken_utc >= @Start AND taken_utc < @End ORDER BY taken_utc",
            UnixTime.RangeParameters(range));

    public static IReadOnlyList<Screenshot> GetTakenBefore(IDbConnection connection, DateTime cutoffUtc) =>
        Query(
            connection,
            SelectScreenshots + " WHERE taken_utc < @Cutoff ORDER BY taken_utc",
            new { Cutoff = UnixTime.ToMilliseconds(cutoffUtc) });

    public static void Delete(IDbConnection connection, IEnumerable<long> ids, IDbTransaction transaction) =>
        connection.Execute("DELETE FROM screenshot WHERE id = @Id", ids.Select(id => new { Id = id }), transaction);

    /// <summary>
    /// Runs <see cref="SelectScreenshots"/> with a condition, so the columns match the tuple below.
    /// </summary>
    private static IReadOnlyList<Screenshot> Query(IDbConnection connection, string sql, object parameters) =>
        [.. connection.Query<(long Id, long Taken, string Path, int Width, int Height)>(sql, parameters)
            .Select(r => new Screenshot(r.Id, UnixTime.FromMilliseconds(r.Taken), r.Path, r.Width, r.Height))];
}
