using ClockShot.Core.Models;
using ClockShot.Core.Projects;

namespace ClockShot.Core.Tagging;

/// <summary>
/// How much tagged time each project and task has.
/// </summary>
public sealed class TagTotals
{
    private readonly Dictionary<(long ProjectId, long? TaskId), TimeSpan> _byTag;

    private TagTotals(Dictionary<(long ProjectId, long? TaskId), TimeSpan> byTag) => _byTag = byTag;

    public static TagTotals Empty { get; } = new([]);

    /// <summary>
    /// All the tagged time, whatever its project and task.
    /// </summary>
    public TimeSpan Total => _byTag.Values.Sum();

    /// <summary>
    /// Totals of the parts of <paramref name="entries"/> inside <paramref name="range"/>.
    /// </summary>
    public static TagTotals Of(IEnumerable<TimeEntry> entries, TimeRange range) =>
        new(entries
            .Select(e => (Tag: (e.ProjectId, e.TaskId), Overlap: e.Range.Intersect(range)))
            .Where(e => e.Overlap is not null)
            .GroupBy(e => e.Tag, e => e.Overlap!.Value.Duration)
            .ToDictionary(g => g.Key, g => g.Sum()));

    /// <summary>
    /// Time tagged to the task.
    /// A null task means the project's General bucket.
    /// </summary>
    public TimeSpan ForTask(long projectId, long? taskId) => _byTag.GetValueOrDefault((projectId, taskId));

    /// <summary>
    /// The time in a row of the client, project and task tree, including every row under it.
    /// </summary>
    public TimeSpan For(CatalogNode node) =>
        node.Tag is { } tag ? ForTask(tag.ProjectId, tag.TaskId) : node.Children.Select(For).Sum();
}
