namespace ClockShot.Core.Projects;

/// <summary>
/// The kinds of row in the client, project and task trees of the Projects tab and the timesheet.
/// </summary>
public enum CatalogRowKind
{
    Client,

    /// <summary>
    /// The group of top-level projects that have no client.
    /// </summary>
    NoClient,

    Project,
    Task,

    /// <summary>
    /// Time tagged to a project without a task.
    /// </summary>
    General,
}
