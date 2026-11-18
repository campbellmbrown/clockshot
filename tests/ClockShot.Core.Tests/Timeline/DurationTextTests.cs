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

    [Theory]
    [InlineData("90 min", 90 * 60)]
    [InlineData("1.5 h", 90 * 60)]
    [InlineData("1,5h", 90 * 60)]
    [InlineData("2h", 2 * 3600)]
    [InlineData("1 h 30 min", 90 * 60)]
    [InlineData("1h30m", 90 * 60)]
    [InlineData("45 s", 45)]
    [InlineData("45", 45 * 60)]
    [InlineData("  3 hours ", 3 * 3600)]
    public void TryParseReadsTypedDurations(string text, int expectedSeconds)
    {
        // Act
        var parsed = DurationText.TryParse(text, out var duration);

        // Assert
        Assert.True(parsed);
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), duration);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1:30")]
    [InlineData("0 min")]
    [InlineData("5 parsecs")]
    [InlineData("-5")]
    [InlineData("nan")]
    [InlineData("infinity")]
    [InlineData("1e20")]
    [InlineData("99999999999")]
    [InlineData("99999999999 h")]
    [InlineData("0.00000001 s")]
    [InlineData("\uFF11h")]
    public void TryParseRejectsUnclearInput(string text)
    {
        // Act
        var parsed = DurationText.TryParse(text, out _);

        // Assert
        Assert.False(parsed);
    }
}
