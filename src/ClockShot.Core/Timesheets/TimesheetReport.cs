namespace ClockShot.Core.Timesheets;

public sealed record TimesheetReport(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<TimesheetColumn> Columns,
    IReadOnlyList<TimesheetRow> Rows,
    IReadOnlyList<TimeSpan> ColumnTotals,
    TimeSpan Total)
{
    public bool IsEmpty => Rows.Count == 0;
}
