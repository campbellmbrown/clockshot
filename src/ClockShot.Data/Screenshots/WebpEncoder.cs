using ClockShot.Core.Screenshots;

using SkiaSharp;

namespace ClockShot.Data.Screenshots;

/// <summary>
/// Turns captured pixels into a WebP image, scaled down to the maximum width.
/// </summary>
internal static class WebpEncoder
{
    public static EncodedImage Encode(CapturedImage image, ScreenshotEncoding encoding)
    {
        var sourceInfo = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var source = SKImage.FromPixelCopy(sourceInfo, WithOpaqueAlpha(image.Pixels), sourceInfo.RowBytes);

        var scale = Math.Min(1.0, (double)encoding.MaxWidthPixels / image.Width);
        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(image.Height * scale));

        using var resized = width == image.Width ? null : Resize(source, width, height);
        using var data = (resized ?? source).Encode(SKEncodedImageFormat.Webp, encoding.Quality);
        return new EncodedImage(data.ToArray(), width, height);
    }

    /// <summary>
    /// A copy of the pixels with every alpha byte at 255.
    /// Captures often leave alpha at 0, and the WebP encoder saves it even for an opaque image.
    /// </summary>
    private static byte[] WithOpaqueAlpha(byte[] pixels)
    {
        var opaque = (byte[])pixels.Clone();
        for (var alphaIndex = CapturedImage.AlphaOffset; alphaIndex < opaque.Length; alphaIndex += CapturedImage.BytesPerPixel)
        {
            opaque[alphaIndex] = byte.MaxValue;
        }

        return opaque;
    }

    private static SKImage Resize(SKImage source, int width, int height)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque));

        // A cubic filter keeps text readable when scaling down.
        source.ScalePixels(bitmap.PeekPixels(), new SKSamplingOptions(SKCubicResampler.Mitchell));
        return SKImage.FromBitmap(bitmap);
    }
}
