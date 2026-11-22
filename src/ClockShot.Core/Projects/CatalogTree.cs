using ClockShot.Core.Models;

namespace ClockShot.Core.Projects;

/// <summary>
/// Builds the tree of clients, projects and tasks that the Projects tab and the timesheet show.
/// </summary>
public static class CatalogTree
{
    /// <summary>
    /// Every client by name, then the "No client" group, each holding its top-level projects.
    /// A project holds its sub-projects, then its General row, then its tasks.
    /// Archived items are included.
    /// Each page leaves out the rows it doesn't show.
    /// </summary>
    public static IReadOnlyList<CatalogNode> Build(ProjectCatalog catalog)
    {
        var clients = catalog.Clients
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => new CatalogNode(CatalogRowKind.Client, c.Id, c.Name, c.IsArchived, Tag: null, ProjectsOf(catalog, c.Id)));
        var noClient = new CatalogNode(
            CatalogRowKind.NoClient,
            Id: null,
            ProjectCatalog.NoClientLabel,
            IsArchived: false,
            Tag: null,
            ProjectsOf(catalog, clientId: null));
        return [.. clients, noClient];
    }

    private static List<CatalogNode> ProjectsOf(ProjectCatalog catalog, long? clientId) =>
        [.. catalog.TopLevelProjectsOf(clientId).Select(p => ProjectNode(catalog, p))];

    private static CatalogNode ProjectNode(ProjectCatalog catalog, Project project)
    {
        var general = new CatalogNode(
            CatalogRowKind.General,
            Id: null,
            ProjectCatalog.GeneralLabel,
            IsArchived: false,
            Tag: (project.Id, null),
            Children: []);
        var tasks = catalog.TasksOf(project.Id, includeArchived: true)
            .Select(t => new CatalogNode(CatalogRowKind.Task, t.Id, t.Name, t.IsArchived, Tag: (project.Id, t.Id), Children: []));
        List<CatalogNode> children = [.. catalog.ChildrenOf(project).Select(p => ProjectNode(catalog, p)), general, .. tasks];
        return new CatalogNode(CatalogRowKind.Project, project.Id, project.Name, project.IsArchived, Tag: null, children);
    }
}
