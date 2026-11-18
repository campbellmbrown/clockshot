namespace ClockShot.Core.Timeline;

/// <summary>
/// Whether the screenshot preview has a screenshot to show, or why not.
/// </summary>
public enum HoverPreviewKind
{
    Screenshot,
    NoScreenshotOfWindow,
    Away,
    Locked,
    NothingRecorded,
}
