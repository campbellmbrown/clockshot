using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class DayBoundariesTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    private static readonly TimeZoneInfo Local = TestTimeZones.WithDaylightSaving;

    [Fact]
    public void DayRunsMidnightToMidnightByDefault()
    {
        // Act
        var range = DayBoundaries.RangeOf(new DateOnly(2026, 11, 10), dayStartHour: 0, Utc);

        // Assert
        Assert.Equal(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc), range.Start);
        Assert.Equal(TimeSpan.FromHours(24), range.Duration);
    }

    [Fact]
    public void DayStartHourShiftsTheDay()
    {
        // Act
        var range = DayBoundaries.RangeOf(new DateOnly(2026, 11, 10), dayStartHour: 4, Utc);

        // Assert
        Assert.Equal(new DateTime(2026, 11, 10, 4, 0, 0, DateTimeKind.Utc), range.Start);
        Assert.Equal(new DateTime(2026, 11, 11, 4, 0, 0, DateTimeKind.Utc), range.End);
    }

    [Fact]
    public void LateNightWorkBelongsToThePreviousDayWhenTheDayStartsAt4()
    {
        // Arrange
        var twoAm = new DateTime(2026, 11, 11, 2, 0, 0, DateTimeKind.Utc);

        // Act
        var day = DayBoundaries.DayOf(twoAm, dayStartHour: 4, Utc);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 10), day);
    }

    [Fact]
    public void LateNightWorkBelongsToItsOwnDayWhenTheDayStartsAtMidnight()
    {
        // Arrange
        var twoAm = new DateTime(2026, 11, 11, 2, 0, 0, DateTimeKind.Utc);

        // Act
        var day = DayBoundaries.DayOf(twoAm, dayStartHour: 0, Utc);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 11), day);
    }

    [Fact]
    public void ADayStartsAtLocalMidnight()
    {
        // Act
        var range = DayBoundaries.RangeOf(new DateOnly(2026, 11, 10), dayStartHour: 0, Local);

        // Assert
        // Midnight at UTC+1 is 23:00 UTC the day before.
        Assert.Equal(new DateTime(2026, 11, 9, 23, 0, 0, DateTimeKind.Utc), range.Start);
    }

    [Fact]
    public void AMomentBelongsToItsLocalDay()
    {
        // Arrange
        // 23:30 UTC is 00:30 the next day at UTC+1.
        var lateEvening = new DateTime(2026, 11, 10, 23, 30, 0, DateTimeKind.Utc);

        // Act
        var day = DayBoundaries.DayOf(lateEvening, dayStartHour: 0, Local);

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 11), day);
    }

    [Fact]
    public void TheDayDaylightSavingStartsIs23Hours()
    {
        // Act
        var range = DayBoundaries.RangeOf(new DateOnly(2026, 3, 29), dayStartHour: 0, Local);

        // Assert
        Assert.Equal(TimeSpan.FromHours(23), range.Duration);
    }

    [Fact]
    public void TheDayDaylightSavingEndsIs25Hours()
    {
        // Act
        var range = DayBoundaries.RangeOf(new DateOnly(2026, 10, 25), dayStartHour: 0, Local);

        // Assert
        Assert.Equal(TimeSpan.FromHours(25), range.Duration);
    }

    [Fact]
    public void DayStartsAnHourLaterWhenDaylightSavingSkipsItsStartHour()
    {
        // Act
        var range = DayBoundaries.RangeOf(new DateOnly(2026, 3, 29), dayStartHour: 2, Local);

        // Assert
        // 02:00 doesn't exist that day. The day starts at 03:00 local time, which is 01:00 UTC.
        Assert.Equal(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc), range.Start);
    }
}
