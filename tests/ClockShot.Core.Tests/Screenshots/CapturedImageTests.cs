using ClockShot.Core.Screenshots;

namespace ClockShot.Core.Tests.Screenshots;

public sealed class CapturedImageTests
{
    [Fact]
    public void KeepsPixelsThatMatchTheSize()
    {
        // Arrange
        var pixels = new byte[3 * 2 * CapturedImage.BytesPerPixel];

        // Act
        var image = new CapturedImage(width: 3, height: 2, pixels);

        // Assert
        Assert.Equal((3, 2), (image.Width, image.Height));
        Assert.Same(pixels, image.Pixels);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void RejectsPixelsThatDoNotMatchTheSize(int extraBytes)
    {
        // Arrange
        var pixels = new byte[(3 * 2 * CapturedImage.BytesPerPixel) + extraBytes];

        // Act
        var thrown = Record.Exception(() => new CapturedImage(width: 3, height: 2, pixels));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(3, 0)]
    [InlineData(-1, 2)]
    public void RejectsASizeThatIsNotPositive(int width, int height)
    {
        // Act
        var thrown = Record.Exception(() => new CapturedImage(width, height, []));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(thrown);
    }
}
