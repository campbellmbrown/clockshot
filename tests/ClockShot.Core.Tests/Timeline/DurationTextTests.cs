using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class DurationTextTests
{
    [Theory]
    [InlineData(40, "40 s")]
    [InlineData(60 * 35, "35 min")]
    [InlineData(60 * 60 * 2, "2 h")]
    [InlineData(60 * 95, "1 h 35 min")]
    public void FormatIsUnambiguous(int seconds, string expected)
    {
        // Act
        var text = DurationText.Format(TimeSpan.FromSeconds(seconds));

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact]
    public void FormatDropsSecondsFromAMinuteUp()
    {
        // Act
        var text = DurationText.Format(TimeSpan.FromSeconds(95));

        // Assert
        Assert.Equal("1 min", text);
    }
}
