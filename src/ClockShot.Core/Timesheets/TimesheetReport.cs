namespace ClockShot.Core.Timesheets;

public sealed record TimesheetReport(
    IReadOnlyList<TimesheetColumn> Columns,
    IReadOnlyList<TimesheetRow> Rows,
    IReadOnlyList<TimeSpan> ColumnTotals,
    TimeSpan Total)
{
    public bool IsEmpty => Rows.Count == 0;
}
