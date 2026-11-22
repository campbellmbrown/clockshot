using ClockShot.App.Controls;
using ClockShot.App.Services;
using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Settings;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Data.Projects;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Projects;

/// <summary>
/// The Projects tab: a searchable tree of clients, projects, sub-projects and tasks with time totals.
/// </summary>
public sealed partial class ProjectsViewModel : PageViewModel, IDisposable
{
    private readonly ProjectRepository _projects;
    private readonly ITimeEntryRepository _entries;
    private readonly DayClock _days;
    private readonly BackgroundWork _background;
    private readonly NewestLoad _newestLoad = new();
    private ProjectCatalog _catalog = ProjectCatalog.Empty;
    private TagTotals _totals = TagTotals.Empty;

    public ProjectsViewModel(
        ProjectRepository projects,
        ITimeEntryRepository entries,
        Tagger tagger,
        ISettingsStore settings,
        DayClock days,
        BackgroundWork background)
        : base("Projects", Icons.Projects)
    {
        _projects = projects;
        _entries = entries;
        _days = days;
        _background = background;
        SelectedPeriod = DatePeriod.ThisWeek;
        settings.Changed += _ => OnUiThread(() => _background.Run(() => LoadAsync()));   // e.g. the day start hour
        projects.Changed += () => OnUiThread(() => _background.Run(() => LoadAsync()));
        tagger.Changed += () => OnUiThread(() => _background.Run(() => LoadAsync()));
        _background.Run(() => LoadAsync());
    }

    /// <summary>
    /// The periods the totals can cover.
    /// </summary>
    public IReadOnlyList<DatePeriod> Periods { get; } = [DatePeriod.ThisWeek, DatePeriod.ThisMonth, DatePeriod.AllTime];

    [ObservableProperty]
    public partial DatePeriod SelectedPeriod { get; set; }

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<CatalogNodeViewModel> Nodes { get; private set; } = [];

    public bool IsEmpty => _catalog.Projects.Count == 0 && _catalog.Clients.Count == 0;

    partial void OnSelectedPeriodChanged(DatePeriod value) => _background.Run(() => LoadAsync());

    partial void OnSearchTextChanged(string? value) => Rebuild();

    /// <summary>
    /// Loads the tree and its totals.
    /// A newer load cancels an unfinished older one.
    /// </summary>
    public async Task LoadAsync()
    {
        var token = _newestLoad.Start();
        var range = SelectedPeriod.DaysFrom(_days.Today) is { } days ? _days.RangeOf(days.First, days.Last) : TimeRange.All;

        try
        {
            var catalog = await _projects.LoadCatalogAsync(token);
            var entries = await _entries.GetAsync(range, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _catalog = catalog;
            _totals = TagTotals.Of(entries, range);
            OnPropertyChanged(nameof(IsEmpty));
            Rebuild();
        }
        catch (OperationCanceledException)
        {
            // A newer load took over.
        }
    }

    public void Dispose() => _newestLoad.Dispose();

    // ---- Tree ----

    /// <summary>
    /// Rebuilds the tree from the catalog, applying the search.
    /// </summary>
    private void Rebuild()
    {
        var words = SearchWords.Of(SearchText);
        Nodes = [.. CatalogTree.Build(_catalog).Select(node => RowFor(node, words)).OfType<CatalogNodeViewModel>()];
        foreach (var node in Nodes)
        {
            Expand(node, all: !words.IsEmpty);
        }
    }

    /// <summary>
    /// The row with the rows under it that are shown, or null when it's hidden.
    /// A row that matches the search shows every row under it.
    /// Any other row shows only when a row under it matches.
    /// </summary>
    private CatalogNodeViewModel? RowFor(CatalogNode node, SearchWords words)
    {
        if (IsHidden(node))
        {
            return null;
        }

        var matches = words.AreAllIn(node.Name);
        var children = node.Children
            .Select(child => RowFor(child, matches ? SearchWords.None : words))
            .OfType<CatalogNodeViewModel>()
            .ToList();
        if (!matches && children.Count == 0)
        {
            return null;
        }

        // "No client" isn't an item the user made.
        // It shows only when it holds projects.
        if (node.Kind == CatalogRowKind.NoClient && children.Count == 0)
        {
            return null;
        }

        return new CatalogNodeViewModel(node, _totals.For(node), children);
    }

    /// <summary>
    /// Archived rows are hidden.
    /// General rows show only when they have time.
    /// </summary>
    private bool IsHidden(CatalogNode node) =>
        node.IsArchived || (node.Kind == CatalogRowKind.General && _totals.For(node) == TimeSpan.Zero);

    /// <summary>
    /// Clients start expanded.
    /// While searching, every row starts expanded to show the matches.
    /// </summary>
    private static void Expand(CatalogNodeViewModel node, bool all)
    {
        node.IsExpanded = all || node.Kind is CatalogRowKind.Client or CatalogRowKind.NoClient;
        foreach (var child in node.Children)
        {
            Expand(child, all);
        }
    }
}
