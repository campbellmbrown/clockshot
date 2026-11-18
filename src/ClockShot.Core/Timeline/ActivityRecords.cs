using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Everything recorded in a time range, for drawing the timeline.
/// </summary>
public sealed record ActivityRecords(
    IReadOnlyList<ActivitySegment> Activities,
    IReadOnlyList<ComputerStateSegment> States,
    IReadOnlyDictionary<long, TrackedApplication> Applications,
    IReadOnlyList<Screenshot> Screenshots)
{
    public static ActivityRecords Empty { get; } = new([], [], new Dictionary<long, TrackedApplication>(), []);
}
