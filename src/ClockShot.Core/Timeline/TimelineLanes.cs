namespace ClockShot.Core.Timeline;

/// <summary>
/// Builds the timeline lanes from recorded activity.
/// </summary>
public static class TimelineLanes
{
    public static IReadOnlyList<TimelineLane> Build(ActivityRecords records) =>
    [
        Computer(records),
    ];

    private static TimelineLane Computer(ActivityRecords records) =>
        new(LaneKind.Computer, "Computer", [.. records.States.Select(s => new TimelineBlock(
            s.Range,
            ComputerStateLabel.Of(s.State),
            new StateColor(s.State),
            new ComputerStateBlock(s.State)))]);
}
