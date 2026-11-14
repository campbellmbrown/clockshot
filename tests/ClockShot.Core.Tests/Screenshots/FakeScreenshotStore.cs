using System.Collections.Concurrent;

using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;

namespace ClockShot.Core.Tests.Screenshots;

/// <summary>
/// Records what is saved, without any files.
/// </summary>
internal sealed class FakeScreenshotStore : IScreenshotStore
{
    public ConcurrentQueue<DateTime> SavedTimes { get; } = new();

    /// <summary>
    /// Each save waits for this task, so a test can keep a save in progress.
    /// </summary>
    public Task SaveFinishes { get; set; } = Task.CompletedTask;

    public async Task<Screenshot?> SaveAsync(
        CapturedImage image,
        DateTime takenUtc,
        ScreenshotEncoding encoding,
        CancellationToken cancellationToken = default)
    {
        SavedTimes.Enqueue(takenUtc);
        await SaveFinishes.WaitAsync(cancellationToken);
        return null;
    }
}
