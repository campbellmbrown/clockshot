using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Timesheets;

namespace ClockShot.Core.Tests.Timesheets;

public sealed class TimesheetExportTests
{
    private static readonly DateOnly Monday = new(2026, 11, 9);

    [Fact]
    public void CsvHasOneLinePerTaskWithDecimalHours()
    {
        // Arrange
        var checkout = Parent(CatalogRowKind.Project, "Checkout", Leaf(hours: [1.25, 0], notes: "Fix \"pay\" button, again"));
        var website = Parent(CatalogRowKind.Project, "Website", checkout);
        var internalProject = Parent(CatalogRowKind.Project, "Internal", Leaf(hours: [0.5, 0], notes: ""));
        TimesheetRow[] rows =
        [
            Parent(CatalogRowKind.Client, "Client A", website),
            Parent(CatalogRowKind.NoClient, ProjectCatalog.NoClientLabel, internalProject),
        ];

        // Act
        var lines = TimesheetExport.ToCsv(Report(DayColumns(count: 2), rows)).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        // Assert
        Assert.Equal("Client,Project,Task,2026-11-09,2026-11-10,Total (hours),Notes", lines[0]);
        Assert.Equal("Client A,Website > Checkout,General,1.25,0,1.25,\"Fix \"\"pay\"\" button, again\"", lines[1]);
        Assert.Equal(",Internal,General,0.5,0,0.5,", lines[2]);
    }

    [Fact]
    public void TabSeparatedTurnsTabsAndLineBreaksInAFieldIntoSpaces()
    {
        // Arrange
        var project = Parent(CatalogRowKind.Project, "Internal", Leaf(hours: [1], notes: "Plan\tdraft\r\nreview"));
        TimesheetRow[] rows = [Parent(CatalogRowKind.NoClient, ProjectCatalog.NoClientLabel, project)];

        // Act
        var lines = TimesheetExport.ToTabSeparated(Report(DayColumns(count: 1), rows)).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        // Assert
        Assert.Equal("\tInternal\tGeneral\t1\t1\tPlan draft review", lines[1]);
    }

    [Fact]
    public void AWeekColumnIsHeadedWithItsFirstAndLastDay()
    {
        // Arrange
        TimesheetColumn[] columns = [new(Monday, Monday.AddDays(6), Label: "9 Nov - 15 Nov")];

        // Act
        var lines = TimesheetExport.ToCsv(Report(columns, rows: [])).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        // Assert
        Assert.Equal("Client,Project,Task,2026-11-09 to 2026-11-15,Total (hours),Notes", lines[0]);
    }

    [Fact]
    public void ARowIsOneTabSeparatedLineWithItsTabsAndLineBreaksTurnedIntoSpaces()
    {
        // Arrange
        var row = Leaf(hours: [1.25, 0], notes: "Plan\tdraft\r\nreview");

        // Act
        var line = TimesheetExport.ToTabSeparatedRow(row);

        // Assert
        Assert.Equal("General\t1.25\t0\t1.25\tPlan draft review", line);
    }

    [Fact]
    public void TheFileNameHoldsTheFirstAndLastDay()
    {
        // Arrange
        var report = Report(DayColumns(count: 7), rows: []);

        // Act
        var name = TimesheetExport.FileName(report);

        // Assert
        Assert.Equal("Timesheet 2026-11-09 to 2026-11-15.csv", name);
    }

    private static TimesheetColumn[] DayColumns(int count) =>
        [.. Enumerable.Range(0, count).Select(i => new TimesheetColumn(Monday.AddDays(i), Monday.AddDays(i), Label: ""))];

    private static TimesheetRow Leaf(double[] hours, string notes)
    {
        TimeSpan[] cells = [.. hours.Select(h => TimeSpan.FromHours(h))];
        return new TimesheetRow(CatalogRowKind.General, ProjectCatalog.GeneralLabel, cells, cells.Sum(), notes, Children: []);
    }

    private static TimesheetRow Parent(CatalogRowKind kind, string name, TimesheetRow child) =>
        new(kind, name, child.Cells, child.Total, Notes: "", Children: [child]);

    private static TimesheetReport Report(TimesheetColumn[] columns, TimesheetRow[] rows)
    {
        TimeSpan[] columnTotals = [.. Enumerable.Range(0, columns.Length).Select(i => rows.Select(r => r.Cells[i]).Sum())];
        return new TimesheetReport(columns[0].From, columns[^1].To, columns, rows, columnTotals, columnTotals.Sum());
    }
}
