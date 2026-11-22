using ClockShot.Core.Projects;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Projects;

/// <summary>
/// One row of the Projects tree.
/// </summary>
public sealed partial class CatalogNodeViewModel(
    CatalogNode node, TimeSpan total, IReadOnlyList<CatalogNodeViewModel> children) : ObservableObject
{
    public CatalogRowKind Kind => node.Kind;

    public string IconKind => CatalogIcons.For(Kind);

    public long? Id => node.Id;

    public string Name => node.Name;

    public TimeSpan Total { get; } = total;

    /// <summary>
    /// The total as text, such as "4 h 25 min", or empty when there's no time.
    /// </summary>
    public string TotalText => Total > TimeSpan.Zero ? DurationText.Format(Total) : "";

    public IReadOnlyList<CatalogNodeViewModel> Children { get; } = children;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>
    /// Clients, projects and tasks can be renamed, archived and deleted.
    /// The "No client" and General rows can't.
    /// </summary>
    public bool IsEditable => Kind is CatalogRowKind.Client or CatalogRowKind.Project or CatalogRowKind.Task;

    public bool IsProject => Kind == CatalogRowKind.Project;

    /// <summary>
    /// Clients and tasks are renamed with a simple prompt.
    /// Projects use the project form instead.
    /// </summary>
    public bool IsRenamable => Kind is CatalogRowKind.Client or CatalogRowKind.Task;

    /// <summary>
    /// New projects can go under a client, the "No client" group, or a project as sub-projects.
    /// </summary>
    public bool CanHoldProjects => Kind is CatalogRowKind.Client or CatalogRowKind.NoClient or CatalogRowKind.Project;

    public CatalogItem? Item => node.Item;
}
