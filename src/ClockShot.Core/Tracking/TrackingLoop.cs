using ClockShot.Core.Settings;

using Microsoft.Extensions.Logging;

namespace ClockShot.Core.Tracking;

/// <summary>
/// The background loop.
/// Once a second it reads the system and records what changed.
/// </summary>
public sealed class TrackingLoop(
    ISystemSampler sampler,
    ITrackingSink sink,
    ISettingsStore settings,
    TimeProvider time,
    ILogger<TrackingLoop> logger) : ITrackingController, IAsyncDisposable
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);

    // Ten missed samples mean the computer slept. Fewer can just be a busy moment.
    private static readonly TimeSpan MaxSampleGap = TimeSpan.FromSeconds(10);

    // Only the loop task uses these, and DisposeAsync once the loop has stopped, so they need no locking.
    // Pause and Resume run on the UI thread and must not read them.
    private readonly ActivityTracker _tracker = new(MaxSampleGap);
    private string? _lastError;

    private readonly CancellationTokenSource _stopping = new();
    private Task? _loop;
    private bool _disposed;

    // The status also says whether tracking is paused.
    // Changing it under this lock stops a status built by the loop from overwriting a newer pause or resume.
    private readonly Lock _statusLock = new();
    private volatile TrackingStatus _status = TrackingStatus.NotStarted;

    public event Action? StatusChanged;

    public TrackingStatus Status => _status;

    public void Start()
    {
        sink.Start();

        // No stop token, because a task cancelled before it starts throws when DisposeAsync awaits it.
        _loop ??= Task.Run(RunAsync, CancellationToken.None);
    }

    public void Pause()
    {
        if (ChangeStatus(_ => TrackingStatus.Paused))
        {
            logger.LogInformation("Tracking paused");
        }
    }

    public void Resume()
    {
        // The tracker belongs to the loop, so its next tick fills in the state and window.
        var resumed = ChangeStatus(status => status.IsPaused
            ? new TrackingStatus(IsPaused: false, State: null, Window: null)
            : status);
        if (resumed)
        {
            logger.LogInformation("Tracking resumed");
        }
    }

    /// <summary>
    /// Stops the loop, closes the open segments and waits for pending writes. Safe to call more than once.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _stopping.CancelAsync().ConfigureAwait(false);
        if (_loop is not null)
        {
            await _loop.ConfigureAwait(false);
        }

        sink.Write(_tracker.Stop(Now()));
        await sink.CompleteAsync().ConfigureAwait(false);
        _stopping.Dispose();
    }

    private async Task RunAsync()
    {
        using var timer = new PeriodicTimer(SampleInterval, time);
        try
        {
            do
            {
                Tick();
            }
            while (await timer.WaitForNextTickAsync(_stopping.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            // Stopping.
        }
    }

    private void Tick()
    {
        try
        {
            var now = Now();
            if (_status.IsPaused)
            {
                if (_tracker.CurrentState is not null)
                {
                    sink.Write(_tracker.Stop(now));
                }

                return;
            }

            var current = settings.Current;
            var awayAfter = TimeSpan.FromMinutes(current.Tracking.AwayAfterMinutes);
            var sample = sampler.Read(now);
            var changes = _tracker.Process(sample, awayAfter);
            sink.Write(changes);
            PublishStatus();

            _lastError = null;
        }
        catch (Exception ex)
        {
            // Log each distinct failure once, not every second.
            if (ex.Message != _lastError)
            {
                logger.LogError(ex, "Tracking sample failed");
            }

            _lastError = ex.Message;
        }
    }

    /// <summary>
    /// Shows the tracker's state and window, unless tracking is paused.
    /// </summary>
    private void PublishStatus() => ChangeStatus(status => status.IsPaused
        ? status
        : new TrackingStatus(IsPaused: false, _tracker.CurrentState, _tracker.CurrentWindow));

    /// <summary>
    /// Sets the status <paramref name="next"/> returns for the current one, under the lock.
    /// Raises <see cref="StatusChanged"/> if the status changed.
    /// </summary>
    /// <returns>True when the status changed.</returns>
    private bool ChangeStatus(Func<TrackingStatus, TrackingStatus> next)
    {
        lock (_statusLock)
        {
            var status = next(_status);
            if (status == _status)
            {
                return false;
            }

            _status = status;
        }

        StatusChanged?.Invoke();
        return true;
    }

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}
