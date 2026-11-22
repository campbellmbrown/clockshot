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

    public string Name => node.Name;

    public TimeSpan Total { get; } = total;

    /// <summary>
    /// The total as text, such as "4 h 25 min", or empty when there's no time.
    /// </summary>
    public string TotalText => Total > TimeSpan.Zero ? DurationText.Format(Total) : "";

    public IReadOnlyList<CatalogNodeViewModel> Children { get; } = children;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }
}
