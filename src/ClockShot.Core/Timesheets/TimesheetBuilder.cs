using System.Globalization;

using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Settings;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Timesheets;

/// <summary>
/// Turns tagged time into a timesheet, with a row per client, project, sub-project and task, and totals.
/// There's one column per day, or per week when the range is longer than <see cref="MaxDayColumns"/> days.
/// Tags that cross the start of a day are split between the days.
/// </summary>
public static class TimesheetBuilder
{
    // A month of day columns still fits on screen.
    // A longer range would be too wide to read.
    private const int MaxDayColumns = 31;

    /// <param name="days">The first and last day, both included.</param>
    public static TimesheetReport Build(
        IReadOnlyList<TimeEntry> entries,
        ProjectCatalog catalog,
        (DateOnly First, DateOnly Last) days,
        DayClock dayClock,
        TimesheetSettings settings)
    {
        var columns = Columns(days);
        var times = TimeByTag(entries, columns, dayClock);
        var rows = new RowBuilder(catalog, times, settings).BuildRows();
        var columnTotals = SumPerColumn(rows, columns.Count);
        return new TimesheetReport(columns, rows, columnTotals, columnTotals.Sum());
    }

    /// <summary>
    /// Rounds one cell.
    /// Zero always stays zero.
    /// </summary>
    public static TimeSpan Round(TimeSpan value, TimesheetSettings settings)
    {
        if (settings.Rounding == RoundingMode.None || value <= TimeSpan.Zero)
        {
            return value;
        }

        var step = TimeSpan.FromMinutes(settings.RoundingMinutes).Ticks;
        var steps = settings.Rounding == RoundingMode.Up
            ? (value.Ticks + step - 1) / step
            : (value.Ticks + step / 2) / step;
        return TimeSpan.FromTicks(steps * step);
    }

    private static List<TimesheetColumn> Columns((DateOnly First, DateOnly Last) days)
    {
        var culture = CultureInfo.CurrentCulture;
        if (days.Last.DayNumber - days.First.DayNumber + 1 <= MaxDayColumns)
        {
            return [.. DaysIn(days.First, days.Last).Select(d => new TimesheetColumn(d, d, d.ToString("ddd d", culture)))];
        }

        var columns = new List<TimesheetColumn>();
        for (var start = days.First; start <= days.Last;)
        {
            var sunday = DatePeriod.WeekOf(start).Last;
            var end = sunday < days.Last ? sunday : days.Last;
            columns.Add(new TimesheetColumn(start, end, $"{start.ToString("d MMM", culture)} - {end.ToString("d MMM", culture)}"));
            start = end.AddDays(1);
        }

        return columns;
    }

    /// <summary>
    /// Adds up each tag's exact time in each column.
    /// </summary>
    private static Dictionary<(long ProjectId, long? TaskId), TagTime> TimeByTag(
        IReadOnlyList<TimeEntry> entries,
        List<TimesheetColumn> columns,
        DayClock dayClock)
    {
        var times = new Dictionary<(long ProjectId, long? TaskId), TagTime>();
        var days = columns.SelectMany((column, index) => DaysIn(column.From, column.To).Select(day => (Day: day, Column: index)));
        foreach (var (day, column) in days)
        {
            var dayRange = dayClock.RangeOf(day);
            foreach (var entry in entries)
            {
                if (entry.Range.Intersect(dayRange) is not { } part)
                {
                    continue;
                }

                var tag = (entry.ProjectId, entry.TaskId);
                if (!times.TryGetValue(tag, out var time))
                {
                    times[tag] = time = new TagTime(columns.Count);
                }

                time.Add(column, part.Duration);
            }
        }

        return times;
    }

    private static IEnumerable<DateOnly> DaysIn(DateOnly first, DateOnly last) =>
        Enumerable.Range(0, last.DayNumber - first.DayNumber + 1).Select(first.AddDays);

    private static List<TimeSpan> SumPerColumn(IReadOnlyList<TimesheetRow> rows, int columnCount) =>
        [.. Enumerable.Range(0, columnCount).Select(i => rows.Select(r => r.Cells[i]).Sum())];

    /// <summary>
    /// One tag's exact time in each column.
    /// </summary>
    private sealed class TagTime(int columnCount)
    {
        public TimeSpan[] Cells { get; } = new TimeSpan[columnCount];

        public void Add(int column, TimeSpan duration) => Cells[column] += duration;
    }

    /// <summary>
    /// Builds the row tree, rounding each cell and leaving out anything with no time.
    /// </summary>
    private sealed class RowBuilder(
        ProjectCatalog catalog,
        Dictionary<(long ProjectId, long? TaskId), TagTime> times,
        TimesheetSettings settings)
    {
        public List<TimesheetRow> BuildRows() => [.. CatalogTree.Build(catalog).Select(Row).OfType<TimesheetRow>()];

        private static TimesheetRow? Parent(CatalogNode node, IReadOnlyList<TimesheetRow> children)
        {
            if (children.Count == 0)
            {
                return null;
            }

            var sums = SumPerColumn(children, children[0].Cells.Count);
            return new TimesheetRow(node.Kind, node.Name, sums, sums.Sum(), children);
        }

        private TimesheetRow? Row(CatalogNode node) =>
            node.Tag is { } tag ? Leaf(node, tag) : Parent(node, [.. node.Children.Select(Row).OfType<TimesheetRow>()]);

        private TimesheetRow? Leaf(CatalogNode node, (long ProjectId, long? TaskId) tag)
        {
            if (!times.TryGetValue(tag, out var time))
            {
                return null;
            }

            List<TimeSpan> cells = [.. time.Cells.Select(cell => Round(cell, settings))];
            if (cells.All(cell => cell == TimeSpan.Zero))
            {
                return null;
            }

            return new TimesheetRow(node.Kind, node.Name, cells, cells.Sum(), Children: []);
        }
    }
}
