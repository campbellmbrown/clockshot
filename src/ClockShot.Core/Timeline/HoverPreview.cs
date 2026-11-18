using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// What the screenshot preview shows for the time under the pointer.
/// </summary>
/// <param name="Kind">Whether there's a screenshot to show, or why not.</param>
/// <param name="Window">The window under the pointer, or null when there's none.</param>
/// <param name="Screenshot">The screenshot to show, or null when there's none.</param>
public sealed record HoverPreview(HoverPreviewKind Kind, WindowBlock? Window, Screenshot? Screenshot)
{
    /// <summary>
    /// Finds the screenshot of the window under the pointer.
    /// When there's no window, the kind says whether the computer was away or locked, or that nothing was recorded.
    /// </summary>
    public static HoverPreview At(IEnumerable<TimelineLane> lanes, ScreenshotIndex screenshots, DateTime time)
    {
        if (BlockAt(lanes, LaneKind.Applications, time) is { Payload: WindowBlock window } block)
        {
            return screenshots.Find(time, block.Range) is { } screenshot
                ? new HoverPreview(HoverPreviewKind.Screenshot, window, screenshot)
                : new HoverPreview(HoverPreviewKind.NoScreenshotOfWindow, window, Screenshot: null);
        }

        var kind = BlockAt(lanes, LaneKind.Computer, time)?.Payload switch
        {
            ComputerStateBlock { State: ComputerState.Away } => HoverPreviewKind.Away,
            ComputerStateBlock { State: ComputerState.Locked } => HoverPreviewKind.Locked,
            _ => HoverPreviewKind.NothingRecorded,
        };
        return new HoverPreview(kind, Window: null, Screenshot: null);
    }

    private static TimelineBlock? BlockAt(IEnumerable<TimelineLane> lanes, LaneKind kind, DateTime time) =>
        lanes.FirstOrDefault(lane => lane.Kind == kind)?.BlockAt(time);
}
