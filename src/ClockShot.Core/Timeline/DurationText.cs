using System.Globalization;
using System.Text.RegularExpressions;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Writes and reads durations such as "1 h 35 min" or "40 s", which can't be mistaken for a time of day.
/// It never writes "1:35", which reads like a time.
/// From a minute up, the seconds are dropped.
/// </summary>
public static partial class DurationText
{
    private const double SecondsPerMinute = 60;
    private const double SecondsPerHour = 3600;

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

    /// <summary>
    /// Reads what a person might type, e.g. "90 min", "1.5 h", "2h", "1 h 30 min", "1h30m" or "45 s".
    /// A bare number means minutes.
    /// Returns false for anything else, including zero and lengths too long to store.
    /// </summary>
    public static bool TryParse(string? text, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().ToLowerInvariant().Replace(',', '.');

        // Plain decimals only, so "nan", "infinity", "1e20" and signs are refused.
        if (double.TryParse(
            normalized,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var bareMinutes))
        {
            return TryFromSeconds(bareMinutes * SecondsPerMinute, out duration);
        }

        var matches = Part().Matches(normalized);
        if (matches.Count == 0 || !CoversWholeText(matches, normalized))
        {
            return false;
        }

        var seconds = 0.0;
        foreach (Match match in matches)
        {
            var value = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
            seconds += value * SecondsIn(match.Groups["unit"].Value);
        }

        return TryFromSeconds(seconds, out duration);
    }

    private static double SecondsIn(string unit) => unit[0] switch
    {
        'h' => SecondsPerHour,
        'm' => SecondsPerMinute,
        _ => 1,
    };

    /// <summary>
    /// Refuses a length too long for a TimeSpan, so a typo can't throw.
    /// It also refuses one shorter than a TimeSpan can hold, such as a billionth of a second.
    /// </summary>
    private static bool TryFromSeconds(double seconds, out TimeSpan duration)
    {
        var fits = seconds < TimeSpan.MaxValue.TotalSeconds;
        duration = fits ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
        return duration > TimeSpan.Zero;
    }

    /// <summary>
    /// True when the matched parts make up the whole text, so nothing else was typed between or around them.
    /// </summary>
    private static bool CoversWholeText(MatchCollection matches, string text) =>
        string.Concat(matches.Select(m => m.Value)).Replace(" ", "") == text.Replace(" ", "");

    /// <summary>
    /// One number and its unit, e.g. "1.5 h", "30min" or "45 sec".
    /// The digits are 0 to 9 only, because double.Parse can't read other scripts' digits.
    /// </summary>
    [GeneratedRegex(@"\s*(?<value>[0-9]+(\.[0-9]+)?)\s*(?<unit>hours?|hrs?|h|minutes?|mins?|m|seconds?|secs?|s)\s*")]
    private static partial Regex Part();
}
