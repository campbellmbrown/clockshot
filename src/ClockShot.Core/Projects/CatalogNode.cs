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
    IReadOnlyList<CatalogNode> Children);
