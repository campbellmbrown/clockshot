using ClockShot.Core.Models;

namespace ClockShot.Core.Tagging;

/// <summary>
/// Stores tagged time.
/// </summary>
public interface ITimeEntryRepository
{
    /// <summary>
    /// Entries overlapping <paramref name="range"/>, sorted by start.
    /// </summary>
    Task<IReadOnlyList<TimeEntry>> GetAsync(TimeRange range, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entries overlapping <paramref name="range"/> or touching either end of it, sorted by start.
    /// </summary>
    Task<IReadOnlyList<TimeEntry>> GetOverlappingOrTouchingAsync(TimeRange range);

    /// <summary>
    /// Applies all changes in one transaction.
    /// Returns them with the ids given to added entries, and with times as they're saved.
    /// </summary>
    Task<TimeEntryChanges> ApplyAsync(TimeEntryChanges changes);

    /// <summary>
    /// The most recently used distinct tags, newest first.
    /// </summary>
    Task<IReadOnlyList<(long ProjectId, long? TaskId)>> GetRecentTagsAsync(int count);
}
