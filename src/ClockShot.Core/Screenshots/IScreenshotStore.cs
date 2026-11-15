using ClockShot.Core.Models;

namespace ClockShot.Core.Screenshots;

public interface IScreenshotStore
{
    /// <summary>
    /// Encodes and saves the image, then indexes it.
    /// Returns null when it was skipped because it is identical to the previous screenshot.
    /// </summary>
    Task<Screenshot?> SaveAsync(
        CapturedImage image,
        DateTime takenUtc,
        ScreenshotEncoding encoding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes screenshots taken before <paramref name="cutoffUtc"/>.
    /// It also deletes temporary files a crash left behind before then, and the folders left empty.
    /// </summary>
    /// <returns>How many screenshots were deleted.</returns>
    /// <exception cref="AggregateException">Some files couldn't be deleted. The rest were.</exception>
    Task<int> DeleteTakenBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
