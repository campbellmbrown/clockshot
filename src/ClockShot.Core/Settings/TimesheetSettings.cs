namespace ClockShot.Core.Settings;

public sealed record TimesheetSettings
{
    /// <summary>
    /// How each task-and-day cell of a timesheet is rounded.
    /// </summary>
    public Timesheets.RoundingMode Rounding { get; init; } = Timesheets.RoundingMode.None;

    /// <summary>
    /// The rounding step in minutes.
    /// </summary>
    public int RoundingMinutes { get; init; } = 15;
}
