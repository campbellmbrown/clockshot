using ClockShot.Core.Models;
using ClockShot.Core.Projects;

namespace ClockShot.Core.Timeline;

/// <summary>
/// What the details panel says a selected block is.
/// </summary>
/// <param name="Heading">The tag, application or computer state.</param>
/// <param name="Subheading">More detail, such as the window title, or empty when there's nothing to add.</param>
public sealed record BlockHeading(string Heading, string Subheading)
{
    private const string PartSeparator = " - ";

    public static BlockHeading For(TimelineBlock block, ProjectCatalog catalog) => block.Payload switch
    {
        TagBlock tag => new(catalog.TagLabel(tag.Entry.ProjectId, tag.Entry.TaskId), TagContext(tag.Entry, catalog)),
        WindowBlock window => new(window.Application.DisplayName, window.Title),
        ComputerStateBlock state => new(ComputerStateLabel.Of(state.State), Subheading: ""),
        _ => new(block.Label, Subheading: ""),
    };

    /// <summary>
    /// The tag's client and parent projects, then its note.
    /// </summary>
    private static string TagContext(TimeEntry entry, ProjectCatalog catalog)
    {
        // The project is never missing, because a project with tagged time can't be deleted.
        var context = catalog.ContextOf(catalog.ProjectById(entry.ProjectId)!);
        return string.Join(PartSeparator, new[] { context, entry.Note ?? "" }.Where(part => part.Length > 0));
    }
}
