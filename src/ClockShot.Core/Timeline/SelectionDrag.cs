using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// One drag on the timeline, which selects time.
/// </summary>
/// <param name="Anchor">
/// The end that stays put while the other end follows the pointer.
/// It's a time, not a screen position, so it stays put when the view scrolls or zooms during the drag.
/// </param>
public sealed record SelectionDrag(DateTime Anchor)
{
    /// <summary>
    /// The range between the anchor and <paramref name="pointerTime"/>, in time order.
    /// Returns null when the pointer is on the anchor, because an empty range isn't a selection.
    /// </summary>
    public TimeRange? RangeTo(DateTime pointerTime) =>
        pointerTime == Anchor
            ? null
            : new TimeRange(TimeMath.Min(pointerTime, Anchor), TimeMath.Max(pointerTime, Anchor));
}
