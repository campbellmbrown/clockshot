using ClockShot.Core.Projects;

namespace ClockShot.Core.Timesheets;

/// <summary>
/// A row stands for a client, project, task or General.
/// A parent's cells are the sums of its children's rounded cells.
/// </summary>
public sealed record TimesheetRow(
    CatalogRowKind Kind,
    string Name,
    IReadOnlyList<TimeSpan> Cells,
    TimeSpan Total,
    IReadOnlyList<TimesheetRow> Children);
