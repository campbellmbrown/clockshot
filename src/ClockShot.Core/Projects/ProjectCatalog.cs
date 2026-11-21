using ClockShot.Core.Models;

namespace ClockShot.Core.Projects;

/// <summary>
/// All clients, projects and tasks, with the lookups the UI needs.
/// It gives each project's path through its client and parents, and each tag's label.
/// It also finds clients and tasks by name.
/// It never changes, so reload it after any change to the projects.
/// </summary>
public sealed class ProjectCatalog
{
    /// <summary>
    /// Separates levels in paths and labels.
    /// </summary>
    public const string Separator = " > ";

    // Stops the walk up the parents if bad data ever makes a project its own ancestor.
    // It's far deeper than anyone nests projects.
    private const int MaxProjectDepth = 100;

    // Typing a name in another case finds the existing item instead of adding a near-duplicate.
    private const StringComparison NameComparison = StringComparison.OrdinalIgnoreCase;

    // Lists are sorted the way the user reads names, ignoring case.
    private static readonly StringComparer DisplayOrder = StringComparer.CurrentCultureIgnoreCase;

    private readonly Dictionary<long, Client> _clients;
    private readonly Dictionary<long, Project> _projects;
    private readonly Dictionary<long, ProjectTask> _tasks;
    private readonly Dictionary<(long ProjectId, long? TaskId), string> _chosenColors;

    public ProjectCatalog(IEnumerable<Client> clients, IEnumerable<Project> projects, IEnumerable<ProjectTask> tasks, IEnumerable<TagColor>? tagColors = null)
    {
        _clients = clients.ToDictionary(c => c.Id);
        _projects = projects.ToDictionary(p => p.Id);
        _tasks = tasks.ToDictionary(t => t.Id);
        _chosenColors = (tagColors ?? []).ToDictionary(c => (c.ProjectId, c.TaskId), c => c.ColorHex);
    }

    public static ProjectCatalog Empty { get; } = new([], [], []);

    public IReadOnlyCollection<Client> Clients => _clients.Values;

    public IReadOnlyCollection<Project> Projects => _projects.Values;

    /// <summary>
    /// A tag's label built from names, for a task that's typed but not created yet.
    /// A blank task name means the project's General bucket.
    /// </summary>
    public static string LabelOf(string projectName, string? taskName) =>
        string.IsNullOrWhiteSpace(taskName) ? projectName : projectName + Separator + taskName.Trim();

    /// <summary>
    /// The automatic colour for a tag label.
    /// The same label always gets the same colour, as application names do.
    /// </summary>
    public static string AutomaticColorOf(string tagLabel) => NameColor.For(tagLabel);

    public Client? ClientById(long? id) => id is { } key ? _clients.GetValueOrDefault(key) : null;

    public Project? ProjectById(long? id) => id is { } key ? _projects.GetValueOrDefault(key) : null;

    public ProjectTask? TaskById(long? id) => id is { } key ? _tasks.GetValueOrDefault(key) : null;

    /// <summary>
    /// The project's parents, top-level first, not including the project itself.
    /// </summary>
    public IReadOnlyList<Project> AncestorsOf(Project project)
    {
        var ancestors = new List<Project>();
        for (var parent = Parent(project); parent is not null && ancestors.Count < MaxProjectDepth; parent = Parent(parent))
        {
            ancestors.Add(parent);
        }

        ancestors.Reverse();
        return ancestors;
    }

    /// <summary>
    /// The client of the project's top-level project.
    /// Sub-projects use that client.
    /// </summary>
    public Client? ClientOf(Project project) => ClientById(TopLevelOf(project).ClientId);

    /// <summary>
    /// Where the project sits without its own name, such as "Client A > Website".
    /// Empty for a top-level project without a client.
    /// </summary>
    public string ContextOf(Project project)
    {
        var ancestorNames = AncestorsOf(project).Select(p => p.Name);
        var names = ClientOf(project) is { } client ? ancestorNames.Prepend(client.Name) : ancestorNames;
        return string.Join(Separator, names);
    }

    /// <summary>
    /// Where the project sits, followed by its own name.
    /// </summary>
    public string PathOf(Project project) =>
        ContextOf(project) is { Length: > 0 } context ? context + Separator + project.Name : project.Name;

    /// <summary>
    /// A tag's label, such as "Website > Login page".
    /// The label of a project's General bucket is just the project's name.
    /// </summary>
    public string TagLabel(long projectId, long? taskId) =>
        // The project is never missing, because a project with tagged time can't be deleted.
        LabelOf(_projects[projectId].Name, TaskById(taskId)?.Name);

    /// <summary>
    /// The colour chosen for a tag, or null when it uses the automatic one.
    /// </summary>
    public string? ChosenColorOf(long projectId, long? taskId) => _chosenColors.GetValueOrDefault((projectId, taskId));

    /// <summary>
    /// A tag's chosen colour, or the automatic colour of its label when none was chosen.
    /// </summary>
    public string ColorOf(long projectId, long? taskId) =>
        ChosenColorOf(projectId, taskId) ?? AutomaticColorOf(TagLabel(projectId, taskId));

    public IReadOnlyList<Project> ChildrenOf(Project parent) =>
        [.. _projects.Values.Where(p => p.ParentId == parent.Id).OrderBy(p => p.Name, DisplayOrder)];

    /// <summary>
    /// The top-level projects of a client.
    /// A null <paramref name="clientId"/> gives the top-level projects without a client.
    /// </summary>
    public IReadOnlyList<Project> TopLevelProjectsOf(long? clientId) =>
        [.. _projects.Values.Where(p => p.ParentId is null && p.ClientId == clientId).OrderBy(p => p.Name, DisplayOrder)];

    public IReadOnlyList<ProjectTask> TasksOf(long projectId, bool includeArchived = false) =>
        [.. _tasks.Values
            .Where(t => t.ProjectId == projectId && (includeArchived || !t.IsArchived))
            .OrderBy(t => t.Name, DisplayOrder)];

    /// <summary>
    /// The project's task with this name, archived or not.
    /// Null when the name is blank or new.
    /// </summary>
    public ProjectTask? TaskNamed(long projectId, string? name) =>
        _tasks.Values.FirstOrDefault(t => t.ProjectId == projectId && IsNamed(t.Name, name));

    /// <summary>
    /// The client with this name, archived or not.
    /// Null when the name is blank or new.
    /// </summary>
    public Client? ClientNamed(string? name) => _clients.Values.FirstOrDefault(c => IsNamed(c.Name, name));

    /// <summary>
    /// True when the project, any of its parents, or its client is archived.
    /// </summary>
    public bool IsArchived(Project project) =>
        project.IsArchived || AncestorsOf(project).Any(p => p.IsArchived) || ClientOf(project)?.IsArchived == true;

    /// <summary>
    /// The projects that can be tagged, ordered by path.
    /// </summary>
    public IReadOnlyList<Project> UnarchivedProjects() =>
        [.. _projects.Values.Where(p => !IsArchived(p)).OrderBy(PathOf, DisplayOrder)];

    /// <summary>
    /// True when <paramref name="name"/>, without its surrounding spaces, is the item's name.
    /// </summary>
    private static bool IsNamed(string itemName, string? name) => string.Equals(itemName, name?.Trim(), NameComparison);

    private Project? Parent(Project project) => project.ParentId is { } id ? _projects.GetValueOrDefault(id) : null;

    private Project TopLevelOf(Project project)
    {
        var ancestors = AncestorsOf(project);
        return ancestors.Count > 0 ? ancestors[0] : project;
    }
}
