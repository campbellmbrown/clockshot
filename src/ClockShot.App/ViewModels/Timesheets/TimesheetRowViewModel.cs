using ClockShot.App.ViewModels.Projects;
using ClockShot.Core.Projects;
using ClockShot.Core.Timeline;
using ClockShot.Core.Timesheets;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Timesheets;

/// <summary>
/// One row of the timesheet grid.
/// A row with children can be collapsed.
/// </summary>
public sealed partial class TimesheetRowViewModel(TimesheetRow row, TimesheetRowViewModel? parent, int depth) : ObservableObject
{
    // Close to the expander arrow's width, so each level's arrow sits clearly to the right of its parent's.
    private const double IndentPerLevel = 18;

    public TimesheetRow Row { get; } = row;

    /// <summary>
    /// The row this one is inside, or null at the top.
    /// </summary>
    public TimesheetRowViewModel? Parent { get; } = parent;

    /// <summary>
    /// Whether every row this one is inside is expanded.
    /// </summary>
    public bool IsShown => Parent is null || (Parent.IsExpanded && Parent.IsShown);

    public string Name => Row.Name;

    /// <summary>
    /// The space before the row's name, which shows how deep it is in the tree.
    /// </summary>
    public double IndentWidth { get; } = depth * IndentPerLevel;

    /// <summary>
    /// Each column's time, such as "1 h 15 min", or blank where there's none.
    /// </summary>
    public IReadOnlyList<string> Cells { get; } = [.. row.Cells.Select(Format)];

    public string TotalText => Format(Row.Total);

    public bool HasChildren => Row.Children.Count > 0;

    /// <summary>
    /// Whether the row is a client or project, which are shown in bold.
    /// </summary>
    public bool IsGroup => Row.Kind is CatalogRowKind.Client or CatalogRowKind.NoClient or CatalogRowKind.Project;

    public string IconKind => CatalogIcons.For(Row.Kind);

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    public static string Format(TimeSpan duration) => duration > TimeSpan.Zero ? DurationText.Format(duration) : "";
}
