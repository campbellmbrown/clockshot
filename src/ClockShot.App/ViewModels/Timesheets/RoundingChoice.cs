using ClockShot.Core.Settings;
using ClockShot.Core.Timesheets;

namespace ClockShot.App.ViewModels.Timesheets;

/// <summary>
/// A rounding choice offered on the Timesheet and Settings tabs.
/// </summary>
/// <param name="Minutes">The rounding step, or null for no rounding.</param>
public sealed record RoundingChoice(string Label, RoundingMode Mode, int? Minutes)
{
    public static IReadOnlyList<RoundingChoice> All { get; } =
    [
        new("No rounding", RoundingMode.None, Minutes: null),
        new("Nearest 6 min", RoundingMode.Nearest, Minutes: 6),
        new("Nearest 15 min", RoundingMode.Nearest, Minutes: 15),
        new("Nearest 30 min", RoundingMode.Nearest, Minutes: 30),
        new("Up to 6 min", RoundingMode.Up, Minutes: 6),
        new("Up to 15 min", RoundingMode.Up, Minutes: 15),
        new("Up to 30 min", RoundingMode.Up, Minutes: 30),
    ];

    /// <summary>
    /// The choice that matches the saved settings, or no rounding if none does.
    /// </summary>
    public static RoundingChoice For(TimesheetSettings settings) =>
        All.FirstOrDefault(choice =>
            choice.Mode == settings.Rounding && (choice.Minutes is null || choice.Minutes == settings.RoundingMinutes))
        ?? All[0];

    /// <summary>
    /// The settings with this choice made.
    /// No rounding keeps the saved step.
    /// </summary>
    public TimesheetSettings ApplyTo(TimesheetSettings settings) =>
        settings with { Rounding = Mode, RoundingMinutes = Minutes ?? settings.RoundingMinutes };
}
