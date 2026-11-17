using System.Globalization;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Picks ruler ticks for the zoom level, aligned to the local clock, such as on the hour.
/// Labels are "HH:mm", or "HH:mm:ss" when they're less than a minute apart.
/// </summary>
public static class TimelineRuler
{
    // Labels closer than this would run into each other.
    private const double MinLabelSpacingPixels = 70;

    /// <summary>
    /// The spacing of major ticks and how many minor steps fit in one major step, smallest first.
    /// </summary>
    private static readonly (TimeSpan Major, int MinorSteps)[] Scales =
    [
        (TimeSpan.FromSeconds(10), 5),
        (TimeSpan.FromSeconds(15), 3),
        (TimeSpan.FromSeconds(30), 6),
        (TimeSpan.FromMinutes(1), 4),
        (TimeSpan.FromMinutes(2), 4),
        (TimeSpan.FromMinutes(5), 5),
        (TimeSpan.FromMinutes(10), 5),
        (TimeSpan.FromMinutes(15), 3),
        (TimeSpan.FromMinutes(30), 6),
        (TimeSpan.FromHours(1), 4),
        (TimeSpan.FromHours(2), 4),
        (TimeSpan.FromHours(3), 3),
        (TimeSpan.FromHours(6), 6),
    ];

    public static IReadOnlyList<RulerTick> Ticks(TimelineViewport viewport, TimeZoneInfo zone)
    {
        var (major, minorSteps) = Scales.FirstOrDefault(
            s => s.Major.TotalSeconds / viewport.SecondsPerPixel >= MinLabelSpacingPixels,
            Scales[^1]);
        var minor = major / minorSteps;
        var labelFormat = major < TimeSpan.FromMinutes(1) ? "HH:mm:ss" : "HH:mm";

        var firstLocal = TimeZoneInfo.ConvertTimeFromUtc(viewport.VisibleStart, zone);
        var lastLocal = TimeZoneInfo.ConvertTimeFromUtc(viewport.VisibleEnd, zone);
        var ticks = new List<RulerTick>();
        for (var local = firstLocal.Date + Floor(firstLocal.TimeOfDay, minor); local <= lastLocal; local += minor)
        {
            // A daylight-saving jump skips this local time.
            if (zone.IsInvalidTime(local))
            {
                continue;
            }

            // Near a daylight-saving change, a local time in range can still fall outside the visible times.
            var utc = TimeZoneInfo.ConvertTimeToUtc(local, zone);
            if (utc < viewport.VisibleStart || utc > viewport.VisibleEnd)
            {
                continue;
            }

            var isMajor = local.TimeOfDay.Ticks % major.Ticks == 0;
            var label = isMajor ? local.ToString(labelFormat, CultureInfo.InvariantCulture) : null;
            ticks.Add(new RulerTick(utc, label));
        }

        return ticks;
    }

    private static TimeSpan Floor(TimeSpan value, TimeSpan step) =>
        TimeSpan.FromTicks(value.Ticks / step.Ticks * step.Ticks);
}
