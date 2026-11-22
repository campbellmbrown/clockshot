namespace ClockShot.Core.Timesheets;

/// <summary>
/// A column holds one day, or one Monday to Sunday week for long ranges.
/// The first and last weeks are cut short at the ends of the range.
/// </summary>
public sealed record TimesheetColumn(DateOnly From, DateOnly To, string Label);
