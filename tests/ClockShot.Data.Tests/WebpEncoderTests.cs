using ClockShot.Core.Screenshots;
using ClockShot.Data.Screenshots;

using SkiaSharp;

namespace ClockShot.Data.Tests;

public sealed class WebpEncoderTests
{
    private static readonly ScreenshotEncoding Encoding = new(MaxWidthPixels: 1920, Quality: 70);

    [Fact]
    public void KeepsAnImageNarrowerThanTheMaximumAtItsSize()
    {
        // Act
        var encoded = WebpEncoder.Encode(TestImages.Gradient(width: 800, height: 600, blue: 10), Encoding);

        // Assert
        using var decoded = SKBitmap.Decode(encoded.Webp);
        Assert.Equal((800, 600), (decoded.Width, decoded.Height));
        Assert.Equal((800, 600), (encoded.Width, encoded.Height));
    }

    [Fact]
    public void ScalesAWiderImageDownToTheMaximumWidth()
    {
        // Act
        var encoded = WebpEncoder.Encode(
            TestImages.Gradient(width: 1000, height: 500, blue: 10),
            Encoding with { MaxWidthPixels = 400 });

        // Assert
        using var decoded = SKBitmap.Decode(encoded.Webp);
        Assert.Equal((400, 200), (decoded.Width, decoded.Height));
        Assert.Equal((400, 200), (encoded.Width, encoded.Height));
    }

    [Fact]
    public void LowerQualityMakesASmallerFile()
    {
        // Arrange
        var image = TestImages.Noise(width: 800, height: 600);

        // Act
        var low = WebpEncoder.Encode(image, Encoding with { Quality = 10 });
        var high = WebpEncoder.Encode(image, Encoding with { Quality = 90 });

        // Assert
        Assert.True(low.Webp.Length < high.Webp.Length);
    }

    [Theory]
    [InlineData(1920)]
    [InlineData(8)]
    public void SavesEveryPixelOpaque(int maxWidthPixels)
    {
        // Arrange
        var transparent = TestImages.Gradient(width: 16, height: 16, blue: 10);

        // Act
        var encoded = WebpEncoder.Encode(transparent, Encoding with { MaxWidthPixels = maxWidthPixels });

        // Assert
        var info = new SKImageInfo(encoded.Width, encoded.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        using var decoded = SKBitmap.Decode(encoded.Webp, info);
        Assert.All(decoded.Pixels, pixel => Assert.Equal(byte.MaxValue, pixel.Alpha));
    }

    [Fact]
    public void QualityOneHundredSavesWithoutLoss()
    {
        // Arrange
        var image = TestImages.Noise(width: 16, height: 16);

        // Act
        var encoded = WebpEncoder.Encode(image, Encoding with { Quality = 100 });

        // Assert
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        using var decoded = SKBitmap.Decode(encoded.Webp, info);
        Assert.Equal(WithoutAlpha(image.Pixels), WithoutAlpha(decoded.Bytes));
    }

    /// <summary>
    /// The encoder makes every pixel opaque, so only the colours can be compared.
    /// </summary>
    private static byte[] WithoutAlpha(byte[] pixels) =>
        [.. pixels.Where((_, index) => index % CapturedImage.BytesPerPixel != CapturedImage.AlphaOffset)];
}
