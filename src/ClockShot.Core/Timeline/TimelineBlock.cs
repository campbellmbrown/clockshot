using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// One bar on the timeline.
/// </summary>
/// <param name="Payload">
/// What the block stands for.
/// Each lane uses its own type, such as <see cref="TagBlock"/>.
/// </param>
public sealed record TimelineBlock(TimeRange Range, string Label, IBlockColor Color, object Payload);
