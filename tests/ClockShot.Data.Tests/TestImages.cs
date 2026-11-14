using ClockShot.Core.Screenshots;

namespace ClockShot.Data.Tests;

/// <summary>
/// Made-up captured images.
/// </summary>
internal static class TestImages
{
    private const byte Red = 200;
    private const int NoiseSeed = 1;

    /// <summary>
    /// An image with a green gradient, which makes it more like a real screenshot than a flat colour.
    /// Images with a different <paramref name="blue"/> have different pixels.
    /// Alpha is left at 0, as screen captures often leave it.
    /// </summary>
    public static CapturedImage Gradient(int width, int height, byte blue)
    {
        var pixels = new byte[width * height * CapturedImage.BytesPerPixel];

        // Each pixel is blue, green, red and alpha, in that order.
        for (var i = 0; i < pixels.Length; i += CapturedImage.BytesPerPixel)
        {
            pixels[i] = blue;
            pixels[i + 1] = (byte)(i / CapturedImage.BytesPerPixel);
            pixels[i + 2] = Red;
        }

        return new CapturedImage(width, height, pixels);
    }

    /// <summary>
    /// An image of random pixels, which compresses as badly as a busy screenshot.
    /// The same size always gives the same pixels.
    /// </summary>
    public static CapturedImage Noise(int width, int height)
    {
        var pixels = new byte[width * height * CapturedImage.BytesPerPixel];
        new Random(NoiseSeed).NextBytes(pixels);
        return new CapturedImage(width, height, pixels);
    }
}
