using ClockShot.Core.Models;

namespace ClockShot.Core.Tagging;

/// <summary>
/// Works out the edits for tagging, clearing and resizing, keeping the rule that tags never overlap.
/// New tagged time replaces whatever was tagged there before, trimming or splitting the old entries.
/// The caller saves the result.
/// </summary>
public static class TimeEntryPlanner
{
    /// <summary>
    /// Puts <paramref name="entry"/> in place as a new entry, ignoring its id.
    /// Touching neighbours with the same project, task and note are joined into it, so tagging next to a tag extends it.
    /// </summary>
    /// <param name="existing">Entries that might overlap or touch the new one.</param>
    public static TimeEntryChanges Place(TimeEntry entry, IReadOnlyList<TimeEntry> existing)
    {
        var cleared = Clear(entry.Range, existing);
        var range = entry.Range;
        var removed = cleared.Removed.ToList();
        var updated = cleared.Updated.ToList();
        var added = cleared.Added.ToList();

        // Pieces left over from the clearing above can be joined too.
        foreach (var neighbour in cleared.ApplyTo(existing))
        {
            if (!SameTag(neighbour, entry) || !Touches(neighbour.Range, range))
            {
                continue;
            }

            range = range.Cover(neighbour.Range);
            Discard(neighbour, removed, updated, added);
        }

        added.Add(entry with { Id = TimeEntry.UnsavedId, Range = range });
        return new TimeEntryChanges(added, updated, removed);
    }

    /// <summary>
    /// Removes tagged time inside <paramref name="range"/>, trimming or splitting entries that cross its edges.
    /// </summary>
    public static TimeEntryChanges Clear(TimeRange range, IReadOnlyList<TimeEntry> existing)
    {
        var added = new List<TimeEntry>();
        var updated = new List<(TimeEntry Before, TimeEntry After)>();
        var removed = new List<TimeEntry>();

        foreach (var old in existing.Where(e => e.Range.Overlaps(range)))
        {
            var keepBefore = old.Range.Start < range.Start;
            var keepAfter = old.Range.End > range.End;
            switch (keepBefore, keepAfter)
            {
                case (false, false):
                    removed.Add(old);
                    break;
                case (true, false):
                    updated.Add((old, old with { Range = new TimeRange(old.Range.Start, range.Start) }));
                    break;
                case (false, true):
                    updated.Add((old, old with { Range = new TimeRange(range.End, old.Range.End) }));
                    break;
                case (true, true):
                    updated.Add((old, old with { Range = new TimeRange(old.Range.Start, range.Start) }));
                    added.Add(old with { Id = TimeEntry.UnsavedId, Range = new TimeRange(range.End, old.Range.End) });
                    break;
            }
        }

        return new TimeEntryChanges(added, updated, removed);
    }

    /// <summary>
    /// Moves an entry's start and end to <paramref name="newRange"/>.
    /// Time it now covers is taken from other entries.
    /// </summary>
    public static TimeEntryChanges Resize(TimeEntry entry, TimeRange newRange, IReadOnlyList<TimeEntry> existing)
    {
        var others = existing.Where(e => e.Id != entry.Id).ToList();
        var cleared = Clear(newRange, others);
        return cleared with { Updated = [.. cleared.Updated, (entry, entry with { Range = newRange })] };
    }

    /// <summary>
    /// Takes a neighbour out of the result, whether it was untouched, updated or newly split off.
    /// </summary>
    private static void Discard(
        TimeEntry neighbour,
        List<TimeEntry> removed,
        List<(TimeEntry Before, TimeEntry After)> updated,
        List<TimeEntry> added)
    {
        if (added.Remove(neighbour))
        {
            return;
        }

        var updateIndex = updated.FindIndex(u => u.After == neighbour);
        if (updateIndex >= 0)
        {
            removed.Add(updated[updateIndex].Before);
            updated.RemoveAt(updateIndex);
            return;
        }

        removed.Add(neighbour);
    }

    private static bool Touches(TimeRange first, TimeRange second) => first.End == second.Start || first.Start == second.End;

    private static bool SameTag(TimeEntry first, TimeEntry second) =>
        first.ProjectId == second.ProjectId
        && first.TaskId == second.TaskId
        && string.Equals(first.Note ?? "", second.Note ?? "", StringComparison.Ordinal);
}
