using ClockShot.Core.Models;
using ClockShot.Core.Tagging;

namespace ClockShot.Core.Tests.Tagging;

public sealed class TimeEntryChangesTests
{
    private const long ProjectId = 1;
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ApplyToDropsRemovedReplacesUpdatedAndAppendsAdded()
    {
        // Arrange
        TimeEntry[] entries = [Entry(1, 0, 10), Entry(2, 10, 20), Entry(3, 20, 30)];
        var changes = new TimeEntryChanges(
            Added: [Entry(TimeEntry.UnsavedId, 40, 50)],
            Updated: [(entries[1], Entry(2, 10, 15))],
            Removed: [entries[2]]);

        // Act
        var after = changes.ApplyTo(entries);

        // Assert
        Assert.Equal([Entry(1, 0, 10), Entry(2, 10, 15), Entry(TimeEntry.UnsavedId, 40, 50)], after);
    }

    [Fact]
    public void ChangesWithNothingInThemAreEmpty()
    {
        // Arrange
        var changes = new TimeEntryChanges(Added: [], Updated: [], Removed: []);

        // Act
        var isEmpty = changes.IsEmpty;

        // Assert
        Assert.True(isEmpty);
    }

    [Fact]
    public void ChangesWithAnyAddedUpdatedOrRemovedEntryAreNotEmpty()
    {
        // Arrange
        var entry = Entry(1, 0, 10);
        var added = new TimeEntryChanges(Added: [entry], Updated: [], Removed: []);
        var updated = new TimeEntryChanges(Added: [], Updated: [(entry, entry)], Removed: []);
        var removed = new TimeEntryChanges(Added: [], Updated: [], Removed: [entry]);

        // Act
        bool[] areEmpty = [added.IsEmpty, updated.IsEmpty, removed.IsEmpty];

        // Assert
        Assert.Equal([false, false, false], areEmpty);
    }

    [Fact]
    public void InverseAddsRemovedEntriesBackAsUnsaved()
    {
        // Arrange
        var changes = new TimeEntryChanges(Added: [], Updated: [], Removed: [Entry(3, 20, 30)]);

        // Act
        var inverse = changes.Inverse();

        // Assert
        Assert.Equal([Entry(TimeEntry.UnsavedId, 20, 30)], inverse.Added);
    }

    private static TimeEntry Entry(long id, int from, int to) =>
        new(id, new TimeRange(Nine.AddMinutes(from), Nine.AddMinutes(to)), ProjectId, TaskId: null, Note: null);
}
