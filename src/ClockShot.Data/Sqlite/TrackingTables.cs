using System.Data;

using ClockShot.Core.Models;
using ClockShot.Core.Tracking;

using Dapper;

namespace ClockShot.Data.Sqlite;

/// <summary>
/// SQL for the tables the tracker writes: application, activity_segment and computer_state_segment.
/// </summary>
internal static class TrackingTables
{
    /// <summary>
    /// Finds the application row for this executable, adding it if it's new. Returns its id.
    /// </summary>
    public static long GetOrAddApplication(
        IDbConnection connection,
        ForegroundWindow window,
        IDbTransaction transaction)
    {
        var id = connection.ExecuteScalar<long?>(
            """
            SELECT id FROM application
            WHERE process_name = @ProcessName AND IFNULL(exe_path, '') = IFNULL(@ExecutablePath, '') COLLATE NOCASE
            """,
            new { window.ProcessName, window.ExecutablePath },
            transaction);
        if (id is { } existing)
        {
            return existing;
        }

        return connection.ExecuteScalar<long>(
            """
            INSERT INTO application (process_name, exe_path, display_name, color)
            VALUES (@ProcessName, @ExecutablePath, @DisplayName, @Color);
            SELECT last_insert_rowid();
            """,
            new
            {
                window.ProcessName,
                window.ExecutablePath,
                window.DisplayName,
                Color = NameColor.For(window.DisplayName),
            },
            transaction);
    }

    public static long InsertActivity(
        IDbConnection connection,
        DateTime startUtc,
        long applicationId,
        string windowTitle,
        IDbTransaction transaction) =>
        connection.ExecuteScalar<long>(
            """
            INSERT INTO activity_segment (start_utc, end_utc, application_id, window_title)
            VALUES (@Start, @Start, @ApplicationId, @WindowTitle);
            SELECT last_insert_rowid();
            """,
            new { Start = UnixTime.ToMilliseconds(startUtc), ApplicationId = applicationId, WindowTitle = windowTitle },
            transaction);

    public static long InsertState(
        IDbConnection connection,
        DateTime startUtc,
        ComputerState state,
        IDbTransaction transaction) =>
        connection.ExecuteScalar<long>(
            """
            INSERT INTO computer_state_segment (start_utc, end_utc, state) VALUES (@Start, @Start, @State);
            SELECT last_insert_rowid();
            """,
            new { Start = UnixTime.ToMilliseconds(startUtc), State = (int)state },
            transaction);

    public static void SetActivityEnd(IDbConnection connection, long id, DateTime endUtc, IDbTransaction transaction) =>
        connection.Execute(
            "UPDATE activity_segment SET end_utc = @End WHERE id = @Id",
            new { Id = id, End = UnixTime.ToMilliseconds(endUtc) },
            transaction);

    public static void SetStateEnd(IDbConnection connection, long id, DateTime endUtc, IDbTransaction transaction) =>
        connection.Execute(
            "UPDATE computer_state_segment SET end_utc = @End WHERE id = @Id",
            new { Id = id, End = UnixTime.ToMilliseconds(endUtc) },
            transaction);

    public static IReadOnlyList<ActivitySegment> GetActivities(IDbConnection connection, TimeRange range) =>
        [.. connection.Query<(long Id, long Start, long End, long ApplicationId, string Title)>(
                """
                SELECT id, start_utc, end_utc, application_id, window_title FROM activity_segment
                WHERE start_utc < @End AND end_utc > @Start ORDER BY start_utc
                """,
                UnixTime.RangeParameters(range))
            .Select(r => new ActivitySegment(r.Id, UnixTime.ToRange(r.Start, r.End), r.ApplicationId, r.Title))];

    public static IReadOnlyList<ComputerStateSegment> GetStates(IDbConnection connection, TimeRange range) =>
        [.. connection.Query<(long Id, long Start, long End, int State)>(
                """
                SELECT id, start_utc, end_utc, state FROM computer_state_segment
                WHERE start_utc < @End AND end_utc > @Start ORDER BY start_utc
                """,
                UnixTime.RangeParameters(range))
            .Select(r => new ComputerStateSegment(r.Id, UnixTime.ToRange(r.Start, r.End), (ComputerState)r.State))];

    public static IReadOnlyList<TrackedApplication> GetApplications(IDbConnection connection) =>
        [.. connection.Query<TrackedApplication>(
            """
            SELECT id, process_name AS ProcessName, exe_path AS ExecutablePath, display_name AS DisplayName,
                color AS ColorHex
            FROM application
            """)];
}
