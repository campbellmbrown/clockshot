using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class LocalTimeTests
{
    private static readonly DateTime NineAmUtc = new(2026, 11, 10, 9, 0, 5, DateTimeKind.Utc);
    private static readonly TimeZoneInfo Zone = TestTimeZones.HalfHourOffset;

    [Fact]
    public void FormatShowsTheLocalHourAndMinute()
    {
        // Act
        var text = LocalTime.Format(NineAmUtc, Zone);

        // Assert
        Assert.Equal("14:30", text);
    }

    [Fact]
    public void FormatWithSecondsShowsTheLocalSeconds()
    {
        // Act
        var text = LocalTime.FormatWithSeconds(NineAmUtc, Zone);

        // Assert
        Assert.Equal("14:30:05", text);
    }
}
