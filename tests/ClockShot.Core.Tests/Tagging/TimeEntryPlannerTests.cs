using ClockShot.Core.Models;
using ClockShot.Core.Tagging;

namespace ClockShot.Core.Tests.Tagging;

public sealed class TimeEntryPlannerTests
{
    private const long ProjectA = 1;
    private const long ProjectB = 2;
    private const long TaskId = 5;

    // Stands in for the ids the database gives added entries, clear of the existing entries' ids.
    private const long FirstSavedId = 10;

    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TaggingOverTheEndOfATagTrimsIt()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 0, 60, ProjectA)];

        // Act
        var changes = TimeEntryPlanner.Place(NewEntry(30, 90, ProjectB), existing);

        // Assert
        Assert.Equal([(0, 30, ProjectA), (30, 90, ProjectB)], After(existing, changes));
    }

    [Fact]
    public void TaggingInsideATagSplitsIt()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 0, 60, ProjectA)];

        // Act
        var changes = TimeEntryPlanner.Place(NewEntry(15, 45, ProjectB), existing);

        // Assert
        Assert.Equal([(0, 15, ProjectA), (15, 45, ProjectB), (45, 60, ProjectA)], After(existing, changes));
    }

    [Fact]
    public void TaggingOverWholeTagsReplacesThem()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 10, 20, ProjectA), Entry(2, 20, 30, ProjectA)];

        // Act
        var changes = TimeEntryPlanner.Place(NewEntry(0, 60, ProjectB), existing);

        // Assert
        Assert.Equal([(0, 60, ProjectB)], After(existing, changes));
        Assert.Equal(2, changes.Removed.Count);
    }

    [Fact]
    public void TaggingNextToTheSameTagJoinsThem()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 0, 30, ProjectA), Entry(2, 60, 90, ProjectA)];

        // Act
        var changes = TimeEntryPlanner.Place(NewEntry(30, 60, ProjectA), existing);

        // Assert
        Assert.Equal([(0, 90, ProjectA)], After(existing, changes));
    }

    [Fact]
    public void TaggingNextToADifferentTaskDoesNotJoin()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 0, 30, ProjectA, TaskId)];

        // Act
        var changes = TimeEntryPlanner.Place(NewEntry(30, 60, ProjectA), existing);

        // Assert
        Assert.Equal([(0, 30, ProjectA), (30, 60, ProjectA)], After(existing, changes));
    }

    [Fact]
    public void TaggingTheSameTagInsideItKeepsOneEntry()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 0, 60, ProjectA)];

        // Act
        var changes = TimeEntryPlanner.Place(NewEntry(15, 45, ProjectA), existing);

        // Assert
        Assert.Equal([(0, 60, ProjectA)], After(existing, changes));
    }

    [Fact]
    public void ClearTrimsTheTagsOnEachSide()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 0, 30, ProjectA), Entry(2, 30, 90, ProjectB)];

        // Act
        var changes = TimeEntryPlanner.Clear(Minutes(20, 40), existing);

        // Assert
        Assert.Equal([(0, 20, ProjectA), (40, 90, ProjectB)], After(existing, changes));
    }

    [Fact]
    public void ResizeTakesTimeFromTheNeighbour()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 0, 30, ProjectA), Entry(2, 30, 90, ProjectB)];

        // Act
        var changes = TimeEntryPlanner.Resize(existing[0], Minutes(0, 50), existing);

        // Assert
        Assert.Equal([(0, 50, ProjectA), (50, 90, ProjectB)], After(existing, changes));
    }

    [Fact]
    public void InverseUndoesTheChanges()
    {
        // Arrange
        TimeEntry[] existing = [Entry(1, 0, 60, ProjectA)];
        var changes = TimeEntryPlanner.Place(NewEntry(15, 45, ProjectB), existing);
        var saved = changes with { Added = [.. changes.Added.Select((e, i) => e with { Id = FirstSavedId + i })] };

        // Act
        var inverse = saved.Inverse();

        // Assert
        Assert.Equal(saved.Added, inverse.Removed);
        Assert.Equal([(existing[0] with { Range = Minutes(0, 15) }, existing[0])], inverse.Updated);
    }

    private static TimeRange Minutes(int from, int to) => new(Nine.AddMinutes(from), Nine.AddMinutes(to));

    private static TimeEntry Entry(long id, int from, int to, long project, long? task = null) =>
        new(id, Minutes(from, to), project, task, Note: null);

    private static TimeEntry NewEntry(int from, int to, long project) => Entry(TimeEntry.UnsavedId, from, to, project);

    /// <summary>
    /// The tagged time after applying the changes, as minutes after 09:00 and the project, sorted by start.
    /// </summary>
    private static List<(int From, int To, long Project)> After(IReadOnlyList<TimeEntry> existing, TimeEntryChanges changes) =>
        [.. changes.ApplyTo(existing)
            .Select(e => (
                From: (int)(e.Range.Start - Nine).TotalMinutes,
                To: (int)(e.Range.End - Nine).TotalMinutes,
                Project: e.ProjectId))
            .OrderBy(e => e.From)];
}
