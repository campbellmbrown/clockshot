using ClockShot.Core.Screenshots;

namespace ClockShot.Core.Tests.Screenshots;

public sealed class ByteSizeTextTests
{
    [Theory]
    [InlineData(0, "0 KB")]
    [InlineData(1_536, "2 KB")]
    [InlineData(1_048_575, "1024 KB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(1_572_864, "1.5 MB")]
    [InlineData(1_073_741_824, "1.0 GB")]
    [InlineData(5_905_580_032, "5.5 GB")]
    public void ShowsTheSizeInTheLargestUnitItReaches(long bytes, string expected)
    {
        // Act
        var text = ByteSizeText.Format(bytes);

        // Assert
        Assert.Equal(expected, text);
    }
}
