namespace ClockShot.Core.Screenshots;

/// <summary>
/// How screenshots are saved.
/// </summary>
/// <param name="MaxWidthPixels">Wider images are scaled down to this width.</param>
/// <param name="Quality">
/// WebP quality from 1 to 100.
/// At 100 the picture is saved without loss.
/// The setting allows 10 to 100.
/// </param>
public sealed record ScreenshotEncoding(int MaxWidthPixels, int Quality);
