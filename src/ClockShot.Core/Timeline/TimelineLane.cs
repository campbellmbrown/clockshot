using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// One row of the timeline.
/// Its blocks must be in time order and must not overlap.
/// The timeline control draws them in that order.
/// </summary>
public sealed class TimelineLane(LaneKind kind, string title, IReadOnlyList<TimelineBlock> blocks)
{
    /// <summary>
    /// Code checks this instead of <see cref="Title"/>, which is only for display.
    /// </summary>
    public LaneKind Kind { get; } = kind;

    public string Title { get; } = title;

    public IReadOnlyList<TimelineBlock> Blocks { get; } = blocks;

    public TimelineBlock? BlockAt(DateTime timeUtc) => Blocks.FirstOrDefault(block => block.Range.Contains(timeUtc));

    public IEnumerable<TimelineBlock> BlocksIn(TimeRange range) => Blocks.Where(block => block.Range.Overlaps(range));
}
