using ClockShot.Core.Screenshots;

namespace ClockShot.Core.Tests.Screenshots;

public sealed class StorageUseTests
{
    [Theory]
    [InlineData(0, 0, "0 KB in 0 screenshots")]
    [InlineData(1_536, 1, "2 KB in 1 screenshot")]
    [InlineData(1_572_864, 1_234, "1.5 MB in 1,234 screenshots")]
    public void DescribesTheSizeAndCount(long bytes, int files, string expected)
    {
        // Arrange
        var use = new StorageUse(bytes, files);

        // Act
        var text = use.Text;

        // Assert
        Assert.Equal(expected, text);
    }
}
