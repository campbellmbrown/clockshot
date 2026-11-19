using ClockShot.Core.Models;

namespace ClockShot.Core.Tagging;

/// <summary>
/// A set of edits to tagged time, applied together.
/// Added entries have <see cref="TimeEntry.UnsavedId"/> until they're saved.
/// <see cref="Updated"/> keeps the before and after versions so the change can be undone.
/// </summary>
public sealed record TimeEntryChanges(
    IReadOnlyList<TimeEntry> Added,
    IReadOnlyList<(TimeEntry Before, TimeEntry After)> Updated,
    IReadOnlyList<TimeEntry> Removed)
{
    public bool IsEmpty => Added.Count == 0 && Updated.Count == 0 && Removed.Count == 0;

    /// <summary>
    /// The changes that put things back as they were.
    /// Removed entries come back as new entries, so the database gives them new ids.
    /// </summary>
    public TimeEntryChanges Inverse() =>
        new(
            Added: [.. Removed.Select(e => e with { Id = TimeEntry.UnsavedId })],
            Updated: [.. Updated.Select(u => (u.After, u.Before))],
            Removed: Added);

    /// <summary>
    /// The entries as they are after these changes.
    /// Removed ones are dropped, updated ones replaced and added ones appended.
    /// Entries are matched by value, so pass the same versions the changes were made from.
    /// </summary>
    public IReadOnlyList<TimeEntry> ApplyTo(IEnumerable<TimeEntry> entries) =>
        [.. entries.Where(e => !Removed.Contains(e)).Select(AfterUpdate).Concat(Added)];

    private TimeEntry AfterUpdate(TimeEntry entry) =>
        Updated.Where(u => u.Before == entry).Select(u => u.After).FirstOrDefault() ?? entry;
}
