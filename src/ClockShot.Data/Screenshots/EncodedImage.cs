namespace ClockShot.Data.Screenshots;

/// <summary>
/// A screenshot encoded as WebP, with the size it was saved at.
/// </summary>
internal sealed record EncodedImage(byte[] Webp, int Width, int Height);
