namespace ClockShot.Core.Screenshots;

/// <summary>
/// The raw pixels of a captured window.
/// Each pixel is 4 bytes in blue, green, red, alpha order.
/// Rows start at the top and have no padding between them.
/// </summary>
public sealed record CapturedImage
{
    public const int BytesPerPixel = 4;

    /// <summary>
    /// Where alpha is within each pixel's bytes.
    /// </summary>
    public const int AlphaOffset = 3;

    /// <exception cref="ArgumentOutOfRangeException">The width or height isn't positive.</exception>
    /// <exception cref="ArgumentException">The pixels don't fill exactly the width and height.</exception>
    public CapturedImage(int width, int height, byte[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        // Encoding reads exactly this many bytes, so any other length means the image is wrong.
        // A long can't overflow, which a huge size could make an int do.
        var expectedLength = (long)width * height * BytesPerPixel;
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException(
                $"A {width} x {height} image needs {expectedLength} bytes, not {pixels.Length}.",
                nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }
}
