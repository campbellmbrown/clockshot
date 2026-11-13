using System.Data;

using ClockShot.Core.Tracking;
using ClockShot.Data.Sqlite;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ClockShot.Data.Tracking;

/// <summary>
/// Saves batches of tracking changes and remembers which records are still open.
/// A batch that fails to save is tried again with the next one, so a short database problem loses nothing.
/// </summary>
internal sealed class TrackingSaver(SqliteConnection connection, ILogger logger)
{
    // The tracker sends a batch about once a second, so this is about ten minutes of failures.
    // Giving up stops the unsaved changes filling memory when the database never recovers.
    public const int GiveUpAfterFailedSaves = 600;

    private readonly List<TrackingChange> _unsaved = [];
    private readonly Dictionary<(string Process, string? Path), long> _applicationIds = [];
    private OpenRecords _open = OpenRecords.None;
    private int _failedSaves;
    private string? _lastError;

    public void Save(IReadOnlyList<TrackingChange> changes)
    {
        _unsaved.AddRange(changes);
        SaveUnsaved();
    }

    /// <summary>
    /// Tries the unsaved changes once more, then saves the end time of the open records.
    /// </summary>
    public void Finish()
    {
        if (_unsaved.Count > 0)
        {
            SaveUnsaved();
        }

        try
        {
            using var transaction = connection.BeginTransaction();
            SaveOpenEnds(transaction);
            transaction.Commit();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saving the end of the open tracking records failed");
        }
    }

    private void SaveUnsaved()
    {
        var lastSaved = _open;
        try
        {
            using var transaction = connection.BeginTransaction();
            foreach (var change in _unsaved)
            {
                Apply(change, transaction);
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            // Go back to what the database holds, so the next try starts from there.
            _open = lastSaved;
            _applicationIds.Clear();
            OnFailedSave(ex);
            return;
        }

        _unsaved.Clear();
        if (_failedSaves > 0)
        {
            logger.LogInformation("Saving tracking data works again after {Attempts} failed attempts", _failedSaves);
        }

        _failedSaves = 0;
        _lastError = null;
    }

    private void OnFailedSave(Exception ex)
    {
        _failedSaves++;

        // Log each distinct failure once, not every second.
        if (ex.Message != _lastError)
        {
            logger.LogError(ex, "Saving tracking data failed. It's tried again with the next changes.");
            _lastError = ex.Message;
        }

        if (_failedSaves < GiveUpAfterFailedSaves)
        {
            return;
        }

        logger.LogError(
            "Gave up saving {Count} tracking changes after {Attempts} failed attempts",
            _unsaved.Count,
            _failedSaves);

        // The lost changes may have ended the open records, so later changes start new ones.
        _unsaved.Clear();
        _open = OpenRecords.None;
        _failedSaves = 0;
        _lastError = null;
    }

    private void Apply(TrackingChange change, IDbTransaction transaction)
    {
        switch (change)
        {
            case ActivityStarted started:
                var applicationId = ApplicationId(started.Window, transaction);
                var newActivityId = TrackingTables.InsertActivity(
                    connection,
                    started.StartUtc,
                    applicationId,
                    started.Window.Title,
                    transaction);
                _open = _open with { ActivityId = newActivityId };
                break;
            case ActivityEnded ended when _open.ActivityId is { } activityId:
                TrackingTables.SetActivityEnd(connection, activityId, ended.EndUtc, transaction);
                _open = _open with { ActivityId = null };
                break;
            case StateStarted started:
                var newStateId = TrackingTables.InsertState(connection, started.StartUtc, started.State, transaction);
                _open = _open with { StateId = newStateId };
                break;
            case StateEnded ended when _open.StateId is { } stateId:
                TrackingTables.SetStateEnd(connection, stateId, ended.EndUtc, transaction);
                _open = _open with { StateId = null };
                break;
            case SegmentsExtended extended:
                _open = _open with { LatestEndUtc = extended.EndUtc };
                if (extended.EndUtc - _open.SavedEndUtc >= TrackingTimings.SaveInterval)
                {
                    SaveOpenEnds(transaction);
                }

                break;
        }
    }

    private void SaveOpenEnds(IDbTransaction transaction)
    {
        if (_open.LatestEndUtc is not { } end)
        {
            return;
        }

        if (_open.ActivityId is { } activityId)
        {
            TrackingTables.SetActivityEnd(connection, activityId, end, transaction);
        }

        if (_open.StateId is { } stateId)
        {
            TrackingTables.SetStateEnd(connection, stateId, end, transaction);
        }

        _open = _open with { SavedEndUtc = end };
    }

    private long ApplicationId(ForegroundWindow window, IDbTransaction transaction)
    {
        var key = (window.ProcessName.ToUpperInvariant(), window.ExecutablePath?.ToUpperInvariant());
        if (!_applicationIds.TryGetValue(key, out var id))
        {
            _applicationIds[key] = id = TrackingTables.GetOrAddApplication(connection, window, transaction);
        }

        return id;
    }

    /// <summary>
    /// The records still being written, and how far their end time has got.
    /// </summary>
    /// <param name="ActivityId">The open activity record, or null when none is open.</param>
    /// <param name="StateId">The open computer-state record, or null when none is open.</param>
    /// <param name="LatestEndUtc">The latest time the open records are known to reach.</param>
    /// <param name="SavedEndUtc">The end time last saved to the open records.</param>
    private readonly record struct OpenRecords(
        long? ActivityId,
        long? StateId,
        DateTime? LatestEndUtc,
        DateTime SavedEndUtc)
    {
        public static OpenRecords None { get; } = new(
            ActivityId: null,
            StateId: null,
            LatestEndUtc: null,
            SavedEndUtc: DateTime.MinValue);
    }
}
