namespace ClockShot.Core.Tests;

/// <summary>
/// Made-up time zones, so tests don't depend on the machine's zone or any real place's rules.
/// </summary>
public static class TestTimeZones
{
    /// <summary>
    /// UTC+1, with daylight saving (UTC+2) from the last Sunday of March 02:00 to the last Sunday of October 03:00.
    /// In 2026, 29 March is a 23-hour day and 25 October is a 25-hour day.
    /// </summary>
    public static readonly TimeZoneInfo WithDaylightSaving = TimeZoneInfo.CreateCustomTimeZone(
        id: "Test Standard Time",
        baseUtcOffset: TimeSpan.FromHours(1),
        displayName: "Test time (with daylight saving)",
        standardDisplayName: "Test Standard Time",
        daylightDisplayName: "Test Daylight Time",
        adjustmentRules:
        [
            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                dateStart: DateTime.MinValue.Date,
                dateEnd: DateTime.MaxValue.Date,
                daylightDelta: TimeSpan.FromHours(1),
                daylightTransitionStart: LastSundayOf(month: 3, hour: 2),
                daylightTransitionEnd: LastSundayOf(month: 10, hour: 3)),
        ]);

    /// <summary>
    /// UTC+5:30, with no daylight saving.
    /// Its hours start half way through UTC's, so a test can tell local times from UTC ones.
    /// </summary>
    public static readonly TimeZoneInfo HalfHourOffset = TimeZoneInfo.CreateCustomTimeZone(
        id: "Test Half Hour Time",
        baseUtcOffset: new TimeSpan(5, 30, 0),
        displayName: "Test time (UTC+5:30)",
        standardDisplayName: "Test Half Hour Time");

    /// <summary>
    /// A clock change at the given hour on the last Sunday of the month.
    /// </summary>
    private static TimeZoneInfo.TransitionTime LastSundayOf(int month, int hour) =>
        TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            // Only the time of day counts, so the date is a placeholder.
            timeOfDay: new DateTime(1, 1, 1, hour, 0, 0, DateTimeKind.Unspecified),
            month: month,
            // Week 5 means the last such day in the month.
            week: 5,
            dayOfWeek: DayOfWeek.Sunday);
}
