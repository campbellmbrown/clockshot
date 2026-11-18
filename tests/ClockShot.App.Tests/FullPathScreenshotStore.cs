using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;

namespace ClockShot.App.Tests;

/// <summary>
/// A screenshot store for tests that only load screenshots.
/// Each screenshot's path is used as it is.
/// </summary>
public sealed class FullPathScreenshotStore : IScreenshotStore
{
    public string FullPath(string imagePath) => imagePath;

    public Task<Screenshot?> SaveAsync(
        CapturedImage image,
        DateTime takenUtc,
        ScreenshotEncoding encoding,
        CancellationToken cancellationToken = default) => throw Unused();

    public Task<int> DeleteTakenBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        throw Unused();

    public Task<StorageUse> GetStorageUseAsync() => throw Unused();

    private static NotSupportedException Unused() => new("This test only loads screenshots.");
}
