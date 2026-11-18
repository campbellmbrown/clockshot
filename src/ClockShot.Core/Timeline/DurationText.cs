using System.Globalization;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Writes durations such as "1 h 35 min" or "40 s", which can't be mistaken for a time of day.
/// It never writes "1:35", which reads like a time.
/// From a minute up, the seconds are dropped.
/// </summary>
public static class DurationText
{
    public static string Format(TimeSpan duration)
    {
        if (duration < TimeSpan.FromMinutes(1))
        {
            return string.Create(CultureInfo.InvariantCulture, $"{(int)duration.TotalSeconds} s");
        }

        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        return (hours, minutes) switch
        {
            (0, _) => string.Create(CultureInfo.InvariantCulture, $"{minutes} min"),
            (_, 0) => string.Create(CultureInfo.InvariantCulture, $"{hours} h"),
            _ => string.Create(CultureInfo.InvariantCulture, $"{hours} h {minutes} min"),
        };
    }
}
