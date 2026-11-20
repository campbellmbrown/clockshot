using ClockShot.Core.Models;
using ClockShot.Core.Tagging;
using ClockShot.Data.Sqlite;

using Dapper;

namespace ClockShot.Data.Tagging;

/// <summary>
/// Tagged time in SQLite.
/// </summary>
public sealed class TimeEntryRepository(SqliteDatabase database) : ITimeEntryRepository
{
    private const string OverlappingSql = """
        SELECT id, start_utc, end_utc, project_id, task_id, note
        FROM time_entry
        WHERE start_utc < @End AND end_utc > @Start
        ORDER BY start_utc
        """;

    private const string OverlappingOrTouchingSql = """
        SELECT id, start_utc, end_utc, project_id, task_id, note
        FROM time_entry
        WHERE start_utc <= @End AND end_utc >= @Start
        ORDER BY start_utc
        """;

    public Task<IReadOnlyList<TimeEntry>> GetAsync(TimeRange range, CancellationToken cancellationToken = default) =>
        QueryAsync(OverlappingSql, range, cancellationToken);

    public Task<IReadOnlyList<TimeEntry>> GetOverlappingOrTouchingAsync(TimeRange range) =>
        QueryAsync(OverlappingOrTouchingSql, range, CancellationToken.None);

    public Task<TimeEntryChanges> ApplyAsync(TimeEntryChanges changes) =>
        Task.Run(() =>
        {
            using var connection = database.Open();
            using var transaction = connection.BeginTransaction();

            foreach (var removed in changes.Removed)
            {
                connection.Execute(
                    "DELETE FROM time_entry WHERE id = @Id",
                    new { removed.Id },
                    transaction);
            }

            foreach (var (_, after) in changes.Updated)
            {
                connection.Execute(
                    """
                    UPDATE time_entry
                    SET start_utc = @Start, end_utc = @End, project_id = @ProjectId, task_id = @TaskId, note = @Note
                    WHERE id = @Id
                    """,
                    Parameters(after),
                    transaction);
            }

            var added = new List<TimeEntry>();
            foreach (var entry in changes.Added)
            {
                var id = connection.ExecuteScalar<long>(
                    """
                    INSERT INTO time_entry (start_utc, end_utc, project_id, task_id, note)
                    VALUES (@Start, @End, @ProjectId, @TaskId, @Note);
                    SELECT last_insert_rowid();
                    """,
                    Parameters(entry),
                    transaction);
                added.Add(AsSaved(entry) with { Id = id });
            }

            transaction.Commit();
            return new TimeEntryChanges(
                Added: added,
                Updated: [.. changes.Updated.Select(u => (u.Before, AsSaved(u.After)))],
                Removed: changes.Removed);
        });

    public Task<IReadOnlyList<(long ProjectId, long? TaskId)>> GetRecentTagsAsync(int count) =>
        Task.Run<IReadOnlyList<(long, long?)>>(() =>
        {
            using var connection = database.Open();
            return [.. connection.Query<(long, long?)>(
                """
                SELECT project_id, task_id
                FROM time_entry
                GROUP BY project_id, task_id
                ORDER BY MAX(id) DESC
                LIMIT @Count
                """,
                new { Count = count })];
        });

    /// <summary>
    /// Runs a query that selects entries using the range's <c>@Start</c> and <c>@End</c>.
    /// </summary>
    private Task<IReadOnlyList<TimeEntry>> QueryAsync(string sql, TimeRange range, CancellationToken cancellationToken) =>
        Task.Run<IReadOnlyList<TimeEntry>>(() =>
        {
            using var connection = database.Open();
            return [.. connection.Query<(long Id, long Start, long End, long ProjectId, long? TaskId, string? Note)>(
                    sql,
                    UnixTime.RangeParameters(range))
                .Select(r => new TimeEntry(
                    r.Id, UnixTime.ToRange(r.Start, r.End), r.ProjectId, r.TaskId, r.Note))];
        }, cancellationToken);

    /// <summary>
    /// The entry with its times cut to the millisecond, as the database stores them.
    /// </summary>
    private static TimeEntry AsSaved(TimeEntry entry) => entry with
    {
        Range = UnixTime.ToRange(
            UnixTime.ToMilliseconds(entry.Range.Start),
            UnixTime.ToMilliseconds(entry.Range.End)),
    };

    private static object Parameters(TimeEntry entry) => new
    {
        entry.Id,
        Start = UnixTime.ToMilliseconds(entry.Range.Start),
        End = UnixTime.ToMilliseconds(entry.Range.End),
        entry.ProjectId,
        entry.TaskId,
        entry.Note,
    };
}
