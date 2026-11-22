namespace ClockShot.Core.Projects;

/// <summary>
/// A client, project or task, identified by kind and id.
/// </summary>
public sealed record CatalogItem(CatalogItemKind Kind, long Id);
