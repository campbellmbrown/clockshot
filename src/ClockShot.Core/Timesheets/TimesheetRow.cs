using ClockShot.Core.Projects;

namespace ClockShot.Core.Timesheets;

/// <summary>
/// A row stands for a client, project, task or General.
/// A parent's cells are the sums of its children's rounded cells.
/// </summary>
/// <param name="Notes">The distinct notes on the row's own tagged time, which only task and General rows have.</param>
public sealed record TimesheetRow(
    CatalogRowKind Kind,
    string Name,
    IReadOnlyList<TimeSpan> Cells,
    TimeSpan Total,
    string Notes,
    IReadOnlyList<TimesheetRow> Children);
