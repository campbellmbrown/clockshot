using System.Text.Json.Serialization;

namespace ClockShot.Core.Timesheets;

/// <summary>
/// How each task-and-day cell of a timesheet is rounded.
/// Each value is saved by the name it's pinned to, so renaming a value doesn't change the settings file.
/// </summary>
public enum RoundingMode
{
    /// <summary>
    /// Cells show exact times.
    /// </summary>
    [JsonStringEnumMemberName("None")]
    None,

    /// <summary>
    /// Each cell is rounded to the nearest step, and halfway rounds up.
    /// </summary>
    [JsonStringEnumMemberName("Nearest")]
    Nearest,

    /// <summary>
    /// Each cell is rounded up to the next step, and zero stays zero.
    /// </summary>
    [JsonStringEnumMemberName("Up")]
    Up,
}
