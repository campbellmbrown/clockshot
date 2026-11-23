using ClockShot.Core.Models;

namespace ClockShot.Core.Tagging;

/// <summary>
/// Tags time, edits and deletes tags, and undoes the most recent of these changes.
/// Only that one change can be undone.
/// All changes keep tags from overlapping (see <see cref="TimeEntryPlanner"/>).
/// </summary>
public sealed class Tagger(ITimeEntryRepository entries)
{
    private TimeEntryChanges? _lastChange;

    /// <summary>
    /// Raised after tagged time changes, including by undo, so screens can reload.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// True when there is a change that <see cref="UndoAsync"/> can undo.
    /// </summary>
    public bool CanUndo => _lastChange is not null;

    /// <summary>
    /// The menu text that says what undo would undo.
    /// Null when there's nothing to undo.
    /// </summary>
    public string? UndoDescription { get; private set; }

    /// <summary>
    /// Tags <paramref name="range"/>, replacing any tags already in it.
    /// </summary>
    public async Task TagAsync(TimeRange range, long projectId, long? taskId, string? note, string undoDescription)
    {
        var entry = new TimeEntry(TimeEntry.UnsavedId, range, projectId, taskId, CleanNote(note));

        // Touching entries are included because the planner joins those with the same tag.
        var existing = await entries.GetOverlappingOrTouchingAsync(range);
        await ApplyAsync(TimeEntryPlanner.Place(entry, existing), undoDescription);
    }

    /// <summary>
    /// Changes an existing entry's project, task or note, keeping its time.
    /// </summary>
    public async Task RetagAsync(TimeEntry entry, long projectId, long? taskId, string? note, string undoDescription)
    {
        var updated = entry with { ProjectId = projectId, TaskId = taskId, Note = CleanNote(note) };

        // Saving it unchanged would take the undo slot from the user's last real change.
        if (updated == entry)
        {
            return;
        }

        await ApplyAsync(new TimeEntryChanges(Added: [], Updated: [(entry, updated)], Removed: []), undoDescription);
    }

    /// <summary>
    /// Moves an entry's start or end.
    /// Time it now covers is taken from neighbouring tags.
    /// </summary>
    public async Task ResizeAsync(TimeEntry entry, TimeRange newRange, string undoDescription)
    {
        var existing = await entries.GetAsync(newRange);
        await ApplyAsync(TimeEntryPlanner.Resize(entry, newRange, existing), undoDescription);
    }

    public Task DeleteAsync(TimeEntry entry, string undoDescription) =>
        ApplyAsync(new TimeEntryChanges(Added: [], Updated: [], Removed: [entry]), undoDescription);

    /// <summary>
    /// Undoes the most recent change.
    /// Afterwards there is nothing left to undo.
    /// </summary>
    public async Task UndoAsync()
    {
        if (_lastChange is not { } last)
        {
            return;
        }

        _lastChange = null;
        UndoDescription = null;
        await entries.ApplyAsync(last.Inverse());
        Changed?.Invoke();
    }

    /// <summary>
    /// The note without surrounding spaces, or null when it's blank.
    /// </summary>
    private static string? CleanNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    private async Task ApplyAsync(TimeEntryChanges changes, string undoDescription)
    {
        if (changes.IsEmpty)
        {
            return;
        }

        _lastChange = await entries.ApplyAsync(changes);
        UndoDescription = undoDescription;
        Changed?.Invoke();
    }
}
