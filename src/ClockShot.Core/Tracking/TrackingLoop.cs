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
    ILogger<TrackingLoop> logger) : IAsyncDisposable
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);

    // Ten missed samples mean the computer slept. Fewer can just be a busy moment.
    private static readonly TimeSpan MaxSampleGap = TimeSpan.FromSeconds(10);

    // Only the loop task uses these, and DisposeAsync once the loop has stopped, so they need no locking.
    private readonly ActivityTracker _tracker = new(MaxSampleGap);
    private string? _lastError;

    private readonly CancellationTokenSource _stopping = new();
    private Task? _loop;
    private bool _disposed;

    public void Start()
    {
        sink.Start();

        // No stop token, because a task cancelled before it starts throws when DisposeAsync awaits it.
        _loop ??= Task.Run(RunAsync, CancellationToken.None);
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
            var current = settings.Current;

            var awayAfter = TimeSpan.FromMinutes(current.Tracking.AwayAfterMinutes);
            var sample = sampler.Read(now);
            var changes = _tracker.Process(sample, awayAfter);
            sink.Write(changes);

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

    private DateTime Now() => time.GetUtcNow().UtcDateTime;
}
