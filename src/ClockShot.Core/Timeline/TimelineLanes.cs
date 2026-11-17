namespace ClockShot.Core.Timeline;

/// <summary>
/// Builds the timeline lanes from recorded activity.
/// </summary>
public static class TimelineLanes
{
    public static IReadOnlyList<TimelineLane> Build(ActivityRecords records) =>
    [
        Computer(records),
        Applications(records),
    ];

    private static TimelineLane Computer(ActivityRecords records) =>
        new(LaneKind.Computer, "Computer", [.. records.States.Select(s => new TimelineBlock(
            s.Range,
            ComputerStateLabel.Of(s.State),
            new StateColor(s.State),
            new ComputerStateBlock(s.State)))]);

    /// <summary>
    /// One block per window, coloured by application, so switching windows within an application is still visible.
    /// </summary>
    private static TimelineLane Applications(ActivityRecords records)
    {
        List<TimelineBlock> blocks = [];
        foreach (var activity in records.Activities)
        {
            // A window whose application row is missing has no name or colour to show.
            if (records.Applications.TryGetValue(activity.ApplicationId, out var application))
            {
                blocks.Add(new TimelineBlock(
                    activity.Range,
                    $"{application.DisplayName} - {activity.WindowTitle}",
                    new HexColor(application.ColorHex),
                    new WindowBlock(application, activity.WindowTitle)));
            }
        }

        return new TimelineLane(LaneKind.Applications, "Applications", blocks);
    }
}
