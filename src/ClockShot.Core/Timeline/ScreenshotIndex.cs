using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// A day's screenshots sorted by time, for finding the one to show while hovering.
/// </summary>
public sealed class ScreenshotIndex
{
    private readonly Screenshot[] _screenshots;

    public ScreenshotIndex(IEnumerable<Screenshot> screenshots)
    {
        _screenshots = [.. screenshots.OrderBy(s => s.TakenUtc)];
    }

    public static ScreenshotIndex Empty { get; } = new([]);

    /// <summary>
    /// The screenshot that best shows the screen at <paramref name="timeUtc"/>.
    /// It's the latest screenshot in <paramref name="within"/> taken at or before <paramref name="timeUtc"/>.
    /// When there's none, it's the earliest one in <paramref name="within"/>.
    /// It's null when <paramref name="within"/> has none.
    /// </summary>
    /// <param name="timeUtc">The hovered time, which must be inside <paramref name="within"/>.</param>
    /// <param name="within">The range of the hovered window block.</param>
    public Screenshot? Find(DateTime timeUtc, TimeRange within) =>
        _screenshots.LastOrDefault(s => within.Contains(s.TakenUtc) && s.TakenUtc <= timeUtc)
        ?? _screenshots.FirstOrDefault(s => within.Contains(s.TakenUtc));
}
