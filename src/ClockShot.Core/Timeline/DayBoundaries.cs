using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Converts between calendar days and UTC time ranges.
/// A day runs from the day start hour, in local time, to the same hour the next day.
/// Someone who works past midnight can set it to 4, so late work stays on the same day.
/// Days around daylight-saving changes are 23 or 25 hours long.
/// </summary>
public static class DayBoundaries
{
    /// <summary>
    /// The UTC range the date covers.
    /// </summary>
    public static TimeRange RangeOf(DateOnly date, int dayStartHour, TimeZoneInfo zone)
    {
        var startLocal = date.ToDateTime(new TimeOnly(dayStartHour, 0));
        return new TimeRange(ToUtc(startLocal, zone), ToUtc(startLocal.AddDays(1), zone));
    }

    /// <summary>
    /// The day a moment belongs to.
    /// </summary>
    public static DateOnly DayOf(DateTime utc, int dayStartHour, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        return DateOnly.FromDateTime(local.AddHours(-dayStartHour));
    }

    /// <summary>
    /// Converts a local wall-clock time to UTC.
    /// A time that a daylight-saving jump skips moves forward an hour.
    /// </summary>
    public static DateTime ToUtc(DateTime local, TimeZoneInfo zone)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }
}
