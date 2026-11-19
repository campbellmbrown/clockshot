namespace ClockShot.Core.Models;

/// <summary>
/// A task within a project.
/// It's named ProjectTask so it doesn't clash with System.Threading.Tasks.Task.
/// </summary>
public sealed record ProjectTask(long Id, long ProjectId, string Name, bool IsArchived);
