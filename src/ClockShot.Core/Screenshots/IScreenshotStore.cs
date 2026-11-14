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
}
