using ClockShot.Core.Models;
using ClockShot.Core.Settings;

using Microsoft.Extensions.Logging;

namespace ClockShot.Core.Screenshots;

/// <summary>
/// Deletes screenshots older than the setting allows, when it starts and every hour after that.
/// </summary>
public sealed class ScreenshotCleanup(
    IScreenshotStore screenshots,
    ISettingsStore settings,
    TimeProvider time,
    ILogger<ScreenshotCleanup> logger) : IAsyncDisposable
{
    // Hourly, not daily, because timers don't count the time the computer is asleep.
    // A daily timer could wait several days on a computer that sleeps every night.
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    // Startup time plus elapsed time, so changing the clock can't delete screenshots early.
    private readonly DateTime _startUtc = time.GetUtcNow().UtcDateTime;
    private readonly long _startTimestamp = time.GetTimestamp();

    private readonly CancellationTokenSource _stopping = new();
    private Task? _loop;
    private bool _disposed;

    // No stop token, because a task cancelled before it starts throws when DisposeAsync awaits it.
    public void Start() => _loop ??= Task.Run(RunAsync, CancellationToken.None);

    /// <summary>
    /// Stops the timer and cancels a cleanup in progress.
    /// It's safe to call more than once.
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

        _stopping.Dispose();
    }

    private async Task RunAsync()
    {
        using var timer = new PeriodicTimer(Interval, time);
        try
        {
            do
            {
                await DeleteOldScreenshotsAsync().ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(_stopping.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            // Stopping. The cleanup runs again at the next start.
        }
    }

    private async Task DeleteOldScreenshotsAsync()
    {
        try
        {
            var keepFor = TimeSpan.FromDays(settings.Current.Screenshots.KeepForDays);
            var sinceStartUtc = _startUtc + time.GetElapsedTime(_startTimestamp);
            var nowUtc = TimeMath.Min(time.GetUtcNow().UtcDateTime, sinceStartUtc);
            var cutoffUtc = nowUtc - keepFor;
            var deleted = await screenshots.DeleteTakenBeforeAsync(cutoffUtc, _stopping.Token).ConfigureAwait(false);
            if (deleted > 0)
            {
                logger.LogInformation("Deleted {Count} screenshots taken before {CutoffUtc:u}", deleted, cutoffUtc);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The next hour tries again.
            logger.LogError(ex, "Deleting old screenshots failed");
        }
    }
}
