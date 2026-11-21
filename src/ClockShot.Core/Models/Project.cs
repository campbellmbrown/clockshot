namespace ClockShot.Core.Models;

/// <summary>
/// A node in the project tree.
/// Top-level projects may belong to a <see cref="Client"/>.
/// Sub-projects use their parent's client.
/// Time tagged without a task goes to the project's General bucket.
/// </summary>
/// <remarks>Projects have no colour, because tags have them (see <see cref="TagColor"/>).</remarks>
public sealed record Project(long Id, long? ClientId, long? ParentId, string Name, bool IsArchived);
