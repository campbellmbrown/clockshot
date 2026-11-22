using ClockShot.Core.Models;
using ClockShot.Core.Settings;

namespace ClockShot.Core.Timeline;

/// <summary>
/// The user's days, which follow the day start hour setting and the local time zone.
/// Every tab uses it, so they all agree on what today and this week mean.
/// </summary>
public sealed class DayClock(TimeProvider time, ISettingsStore settings)
{
    public TimeZoneInfo Zone => time.LocalTimeZone;

    public DateTime NowUtc => time.GetUtcNow().UtcDateTime;

    public DateOnly Today => DayBoundaries.DayOf(NowUtc, DayStartHour, Zone);

    /// <summary>
    /// The local hour, from 0 to 23, at which a new day starts.
    /// </summary>
    private int DayStartHour => settings.Current.General.DayStartHour;

    public TimeRange RangeOf(DateOnly day) => DayBoundaries.RangeOf(day, DayStartHour, Zone);

    /// <summary>
    /// The time from the start of the first day to the end of the last day.
    /// </summary>
    public TimeRange RangeOf(DateOnly first, DateOnly last) => new(RangeOf(first).Start, RangeOf(last).End);
}
