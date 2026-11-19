namespace ClockShot.Core.Models;

/// <summary>
/// A span of time tagged with a project.
/// A null <see cref="TaskId"/> means the project's General bucket.
/// </summary>
public sealed record TimeEntry(long Id, TimeRange Range, long ProjectId, long? TaskId, string? Note)
{
    /// <summary>
    /// The id of an entry that isn't saved yet.
    /// The database numbers saved entries from 1.
    /// </summary>
    public const long UnsavedId = 0;
}
