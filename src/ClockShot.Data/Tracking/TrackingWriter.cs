using System.Threading.Channels;

using ClockShot.Core.Tracking;
using ClockShot.Data.Sqlite;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ClockShot.Data.Tracking;

/// <summary>
/// Saves tracking changes to the database on a background task, so the sampling loop never waits on disk.
/// It is the only writer of the tracking tables. The end time of the open segments is saved every
/// <see cref="TrackingTimings.SaveInterval"/>, so a crash loses at most that much time.
/// </summary>
public sealed class TrackingWriter : ITrackingSink
{
    private readonly SqliteDatabase _database;
    private readonly ILogger<TrackingWriter> _logger;
    private readonly Channel<IReadOnlyList<TrackingChange>> _queue =
        Channel.CreateUnbounded<IReadOnlyList<TrackingChange>>(new UnboundedChannelOptions { SingleReader = true });
    private Task? _consumer;

    public TrackingWriter(SqliteDatabase database, ILogger<TrackingWriter> logger)
    {
        _database = database;
        _logger = logger;
    }

    public void Start() => StartSaving();

    public void Write(IReadOnlyList<TrackingChange> changes)
    {
        if (changes.Count > 0)
        {
            _queue.Writer.TryWrite(changes);
        }
    }

    public async Task CompleteAsync()
    {
        _queue.Writer.TryComplete();

        // Starting here as well saves the queued changes even if Start was never called.
        await StartSaving().ConfigureAwait(false);
    }

    private Task StartSaving() => _consumer ??= Task.Run(SaveQueuedChangesAsync);

    private async Task SaveQueuedChangesAsync()
    {
        using var connection = TryOpen();
        if (connection is null)
        {
            return;
        }

        var saver = new TrackingSaver(connection, _logger);
        await foreach (var changes in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            saver.Save(changes);
        }

        saver.Finish();
    }

    private SqliteConnection? TryOpen()
    {
        try
        {
            return _database.Open();
        }
        catch (SqliteException ex)
        {
            _logger.LogError(
                ex,
                "Tracking data won't be saved until the app restarts, because the database can't be opened");

            // Later changes are dropped instead of piling up in memory.
            _queue.Writer.TryComplete();
            return null;
        }
    }
}
