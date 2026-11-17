using ClockShot.Core.Models;
using ClockShot.Core.Timeline;
using ClockShot.Data.Sqlite;

namespace ClockShot.Data.Tracking;

/// <summary>
/// Reads recorded activity and computer state for the timeline.
/// </summary>
public sealed class ActivityHistory(SqliteDatabase database) : IActivityHistory
{
    public Task<ActivityRecords> LoadAsync(TimeRange range, CancellationToken cancellationToken = default) =>
        // Microsoft.Data.Sqlite reads synchronously, so this keeps the read off the UI thread.
        Task.Run(() =>
        {
            using var connection = database.Open();
            return new ActivityRecords(
                TrackingTables.GetActivities(connection, range),
                TrackingTables.GetStates(connection, range),
                TrackingTables.GetApplications(connection).ToDictionary(a => a.Id));
        }, cancellationToken);

    public Task<IReadOnlyList<TrackedApplication>> GetApplicationsAsync() =>
        Task.Run(() =>
        {
            using var connection = database.Open();
            return TrackingTables.GetApplications(connection);
        });
}
