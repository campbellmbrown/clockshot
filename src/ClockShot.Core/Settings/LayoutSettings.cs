namespace ClockShot.Core.Settings;

/// <summary>
/// Sizes the user has dragged panels to, so the window looks the same next time.
/// </summary>
public sealed record LayoutSettings
{
    /// <summary>
    /// Height of the timeline area on the Timeline tab, in pixels.
    /// </summary>
    public double TimelineHeight { get; init; } = 170;

    /// <summary>
    /// Width of the details panel on the Timeline tab, in pixels.
    /// </summary>
    public double DetailsPanelWidth { get; init; } = 360;
}
