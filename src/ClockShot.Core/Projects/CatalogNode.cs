namespace ClockShot.Core.Projects;

/// <summary>
/// One row of the tree of clients, projects and tasks.
/// </summary>
/// <param name="Id">The client's, project's or task's id, or null for the "No client" and General rows.</param>
/// <param name="Tag">The tag that a task or General row's time has, or null for other rows.</param>
public sealed record CatalogNode(
    CatalogRowKind Kind,
    long? Id,
    string Name,
    bool IsArchived,
    (long ProjectId, long? TaskId)? Tag,
    IReadOnlyList<CatalogNode> Children)
{
    /// <summary>
    /// The client, project or task the row stands for, or null for the "No client" and General rows.
    /// </summary>
    public CatalogItem? Item => (Kind, Id) switch
    {
        (CatalogRowKind.Client, { } id) => new CatalogItem(CatalogItemKind.Client, id),
        (CatalogRowKind.Project, { } id) => new CatalogItem(CatalogItemKind.Project, id),
        (CatalogRowKind.Task, { } id) => new CatalogItem(CatalogItemKind.Task, id),
        _ => null,
    };
}
