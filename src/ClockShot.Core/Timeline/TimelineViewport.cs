using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Which part of the day is visible, and how it maps to pixels.
/// It never changes, so zooming and panning return a new viewport.
/// A viewport always stays inside <see cref="Bounds"/>.
/// It shows at least <see cref="MinVisibleDuration"/> and at most the whole day.
/// </summary>
public readonly record struct TimelineViewport
{
    /// <summary>
    /// The most zoomed-in view shows this much time across the full width.
    /// A minute across is close enough to see each switch between windows, so the zoom stops there.
    /// </summary>
    public static readonly TimeSpan MinVisibleDuration = TimeSpan.FromMinutes(1);

    // The drawing area is 0 pixels wide before its first layout, and a width of 0 makes the time per pixel infinite.
    private const double MinWidth = 1;

    private TimelineViewport(TimeRange bounds, DateTime visibleStart, double secondsPerPixel, double width)
    {
        Bounds = bounds;
        Width = width;
        SecondsPerPixel = Math.Clamp(secondsPerPixel, MinSecondsPerPixel, MaxSecondsPerPixel);
        var latestStart = bounds.End.AddSeconds(-Width * SecondsPerPixel);
        VisibleStart = TimeMath.Max(bounds.Start, TimeMath.Min(visibleStart, latestStart));
    }

    /// <summary>
    /// The whole range that can be shown, which is the day.
    /// </summary>
    public TimeRange Bounds { get; }

    public DateTime VisibleStart { get; }

    public double SecondsPerPixel { get; }

    /// <summary>
    /// The width of the drawing area in pixels.
    /// </summary>
    public double Width { get; }

    public DateTime VisibleEnd => VisibleStart.AddSeconds(Width * SecondsPerPixel);

    public TimeRange Visible => new(VisibleStart, VisibleEnd);

    /// <summary>
    /// True when the whole day is visible.
    /// </summary>
    public bool IsFit => SecondsPerPixel >= MaxSecondsPerPixel;

    private double MaxSecondsPerPixel => Bounds.Duration.TotalSeconds / Width;

    private double MinSecondsPerPixel => Math.Min(MinVisibleDuration.TotalSeconds / Width, MaxSecondsPerPixel);

    /// <summary>
    /// A viewport showing all of <paramref name="bounds"/>.
    /// </summary>
    public static TimelineViewport Fit(TimeRange bounds, double width)
    {
        width = Math.Max(MinWidth, width);
        return new TimelineViewport(bounds, bounds.Start, bounds.Duration.TotalSeconds / width, width);
    }

    public double XOf(DateTime timeUtc) => (timeUtc - VisibleStart).TotalSeconds / SecondsPerPixel;

    public DateTime TimeAt(double x) => VisibleStart.AddSeconds(x * SecondsPerPixel);

    /// <summary>
    /// Zooms by <paramref name="factor"/>, keeping the time under <paramref name="x"/> in place.
    /// A factor above 1 zooms in, and one below 1 zooms out.
    /// </summary>
    public TimelineViewport ZoomAt(double x, double factor)
    {
        var anchor = TimeAt(x);
        var secondsPerPixel = Math.Clamp(SecondsPerPixel / factor, MinSecondsPerPixel, MaxSecondsPerPixel);
        return new TimelineViewport(Bounds, anchor.AddSeconds(-x * secondsPerPixel), secondsPerPixel, Width);
    }

    /// <summary>
    /// Moves the view right by <paramref name="pixels"/>, or left when it's negative.
    /// </summary>
    public TimelineViewport PanBy(double pixels) =>
        new(Bounds, VisibleStart.AddSeconds(pixels * SecondsPerPixel), SecondsPerPixel, Width);
}
