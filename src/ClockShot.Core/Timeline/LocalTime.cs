using System.Globalization;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Shows stored UTC times as 24-hour wall-clock times in the user's time zone.
/// </summary>
public static class LocalTime
{
    public static string Format(DateTime utc, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, zone).ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string FormatWithSeconds(DateTime utc, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, zone).ToString("HH:mm:ss", CultureInfo.InvariantCulture);
}
