namespace ClockShot.Core.Timesheets;

/// <summary>
/// How each task-and-day cell of a timesheet is rounded.
/// </summary>
public enum RoundingMode
{
    /// <summary>
    /// Cells show exact times.
    /// </summary>
    None,

    /// <summary>
    /// Each cell is rounded to the nearest step, and halfway rounds up.
    /// </summary>
    Nearest,

    /// <summary>
    /// Each cell is rounded up to the next step, and zero stays zero.
    /// </summary>
    Up,
}
