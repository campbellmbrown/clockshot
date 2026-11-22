using System.Globalization;

using ClockShot.Core.Projects;

namespace ClockShot.Core.Timesheets;

/// <summary>
/// Writes a timesheet as a flat table for spreadsheets and timesheet systems, with one line per task or General row.
/// Its columns are Client, Project, Task, one per day or week, Total and Notes.
/// Times are decimal hours, such as "1.25", which spreadsheets add up and which can't be mistaken for a time of day.
/// </summary>
public static class TimesheetExport
{
    // The CSV standard ends each line with CRLF.
    private const string LineEnd = "\r\n";

    // Spreadsheets read ISO dates the same way in every locale.
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Writes the timesheet comma-separated, with quotes where needed, for saving as a CSV file.
    /// </summary>
    public static string ToCsv(TimesheetReport report) => Write(report, separator: ',', EscapeCsv);

    /// <summary>
    /// Writes the timesheet tab-separated, for pasting into spreadsheets and web timesheets.
    /// </summary>
    public static string ToTabSeparated(TimesheetReport report) => Write(report, separator: '\t', EscapeTabSeparated);

    /// <summary>
    /// Writes one row's name, hours per column, total and notes as a tab-separated line, for pasting.
    /// </summary>
    public static string ToTabSeparatedRow(TimesheetRow row)
    {
        string[] fields = [row.Name, .. row.Cells.Select(Hours), Hours(row.Total), row.Notes];
        return string.Join('\t', fields.Select(EscapeTabSeparated));
    }

    /// <summary>
    /// The name suggested for the timesheet's CSV file, with its first and last day.
    /// </summary>
    public static string FileName(TimesheetReport report) => $"Timesheet {Date(report.From)} to {Date(report.To)}.csv";

    private static string Hours(TimeSpan duration) => duration.TotalHours.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Date(DateOnly day) => day.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static string Write(TimesheetReport report, char separator, Func<string, string> escape) =>
        string.Concat(Lines(report).Select(fields => string.Join(separator, fields.Select(escape)) + LineEnd));

    private static IEnumerable<IEnumerable<string>> Lines(TimesheetReport report)
    {
        yield return ["Client", "Project", "Task", .. report.Columns.Select(ColumnHeader), "Total (hours)", "Notes"];

        foreach (var group in report.Rows)
        {
            var client = group.Kind == CatalogRowKind.NoClient ? "" : group.Name;
            foreach (var (path, leaf) in Leaves(group.Children, parentPath: ""))
            {
                yield return [client, path, leaf.Name, .. leaf.Cells.Select(Hours), Hours(leaf.Total), leaf.Notes];
            }
        }
    }

    /// <summary>
    /// Every task or General row, with its project path.
    /// </summary>
    private static IEnumerable<(string ProjectPath, TimesheetRow Leaf)> Leaves(IEnumerable<TimesheetRow> rows, string parentPath)
    {
        foreach (var row in rows)
        {
            if (row.Children.Count == 0)
            {
                yield return (parentPath, row);
                continue;
            }

            var path = parentPath.Length == 0 ? row.Name : parentPath + ProjectCatalog.Separator + row.Name;
            foreach (var leaf in Leaves(row.Children, path))
            {
                yield return leaf;
            }
        }
    }

    private static string ColumnHeader(TimesheetColumn column) =>
        column.From == column.To ? Date(column.From) : $"{Date(column.From)} to {Date(column.To)}";

    /// <summary>
    /// Quotes a field that holds a comma, a quote or a line break, and doubles the quotes inside it.
    /// </summary>
    private static string EscapeCsv(string field)
    {
        if (field.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return field;
        }

        return $"\"{field.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    // A tab or line break inside a pasted field would start a new cell or row.
    private static string EscapeTabSeparated(string field) => field.Replace('\t', ' ').ReplaceLineEndings(" ");
}
