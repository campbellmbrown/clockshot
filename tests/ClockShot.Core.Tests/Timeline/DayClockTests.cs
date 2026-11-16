using ClockShot.Core.Models;
using ClockShot.Core.Settings;
using ClockShot.Core.Tests.Settings;
using ClockShot.Core.Timeline;

using Microsoft.Extensions.Time.Testing;

namespace ClockShot.Core.Tests.Timeline;

public sealed class DayClockTests
{
    private static readonly DateOnly Monday = new(2026, 11, 9);

    [Fact]
    public void TodayIsTheLocalDate()
    {
        // Arrange
        var days = Clock(utcNow: new DateTime(2026, 11, 9, 22, 0, 0, DateTimeKind.Utc), dayStartHour: 0);

        // Act
        var today = days.Today;

        // Assert
        Assert.Equal(Monday.AddDays(1), today);
    }

    [Fact]
    public void TodayStaysTheDayBeforeUntilTheDayStartHour()
    {
        // Arrange
        var days = Clock(utcNow: new DateTime(2026, 11, 9, 22, 0, 0, DateTimeKind.Utc), dayStartHour: 4);

        // Act
        var today = days.Today;

        // Assert
        Assert.Equal(Monday, today);
    }

    [Fact]
    public void ADayRunsFromItsStartHourToTheSameHourTheNextDay()
    {
        // Arrange
        var days = Clock(utcNow: new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc), dayStartHour: 4);

        // Act
        var range = days.RangeOf(Monday);

        // Assert
        // 04:00 on Monday at UTC+5:30 is 22:30 on Sunday in UTC.
        var mondayFourAmLocal = new DateTime(2026, 11, 8, 22, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new TimeRange(mondayFourAmLocal, mondayFourAmLocal.AddDays(1)), range);
    }

    /// <summary>
    /// A clock in a made-up zone half an hour off the hour, so local and UTC dates differ in the evening.
    /// </summary>
    private static DayClock Clock(DateTime utcNow, int dayStartHour)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(utcNow));
        time.SetLocalTimeZone(TestTimeZones.HalfHourOffset);

        var settings = new FakeSettingsStore();
        settings.Save(new AppSettings { General = new GeneralSettings { DayStartHour = dayStartHour } });

        return new DayClock(time, settings);
    }
}
