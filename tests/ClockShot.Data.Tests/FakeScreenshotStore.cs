using System.Collections.Concurrent;
using System.Threading.Channels;

using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;

namespace ClockShot.Data.Tests;

/// <summary>
/// Records what is saved and deleted, without any files.
/// </summary>
internal sealed class FakeScreenshotStore : IScreenshotStore
{
    public ConcurrentQueue<DateTime> SavedTimes { get; } = new();

    /// <summary>
    /// The cutoff of each delete, which a test can await because deletes run in the background.
    /// </summary>
    public Channel<DateTime> DeleteCutoffs { get; } = Channel.CreateUnbounded<DateTime>();

    /// <summary>
    /// Each save waits for this task, so a test can keep a save in progress.
    /// </summary>
    public Task SaveFinishes { get; set; } = Task.CompletedTask;

    /// <summary>
    /// Each delete waits for this task, so a test can make deletes fail.
    /// </summary>
    public Task DeleteFinishes { get; set; } = Task.CompletedTask;

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

    public async Task<int> DeleteTakenBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        DeleteCutoffs.Writer.TryWrite(cutoffUtc);
        await DeleteFinishes.WaitAsync(cancellationToken);
        return 0;
    }

    public string FullPath(string imagePath) => throw new NotSupportedException();

    public Task<StorageUse> GetStorageUseAsync() => throw new NotSupportedException();
}
