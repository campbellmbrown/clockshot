using ClockShot.Core.Models;
using ClockShot.Core.Tagging;

namespace ClockShot.Core.Tests.Tagging;

public sealed class TaggerTests
{
    private const long ProjectA = 1;
    private const long ProjectB = 2;
    private const long TaskId = 7;
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryTimeEntries _store = new();
    private readonly Tagger _tagger;

    public TaggerTests() => _tagger = new Tagger(_store);

    [Fact]
    public async Task TagReplacesOverlappingTagsAndAnnouncesTheChange()
    {
        // Arrange
        var changes = 0;
        _tagger.Changed += () => changes++;
        await TagAsync(Minutes(0, 60), ProjectA);

        // Act
        await TagAsync(Minutes(30, 90), ProjectB);

        // Assert
        Assert.Equal([(Minutes(0, 30), ProjectA), (Minutes(30, 90), ProjectB)], Tags());
        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task TagJoinsATouchingTagWithTheSameProject()
    {
        // Arrange
        await TagAsync(Minutes(0, 30), ProjectA);

        // Act
        await TagAsync(Minutes(30, 60), ProjectA);

        // Assert
        Assert.Equal([(Minutes(0, 60), ProjectA)], Tags());
    }

    [Fact]
    public async Task UndoDescriptionSaysWhatUndoWillUndo()
    {
        // Act
        await TagAsync(Minutes(0, 60), ProjectA, undoDescription: "Undo tagging 09:00 - 10:00");

        // Assert
        Assert.Equal("Undo tagging 09:00 - 10:00", _tagger.UndoDescription);
    }

    [Fact]
    public async Task UndoRestoresWhatWasThere()
    {
        // Arrange
        await TagAsync(Minutes(0, 60), ProjectA);
        await TagAsync(Minutes(15, 45), ProjectB);

        // Act
        await _tagger.UndoAsync();

        // Assert
        Assert.Equal([(Minutes(0, 60), ProjectA)], Tags());
        Assert.False(_tagger.CanUndo);
    }

    [Fact]
    public async Task ASecondUndoChangesNothing()
    {
        // Arrange
        await TagAsync(Minutes(0, 60), ProjectA);
        await TagAsync(Minutes(15, 45), ProjectB);
        await _tagger.UndoAsync();

        // Act
        await _tagger.UndoAsync();

        // Assert
        Assert.Equal([(Minutes(0, 60), ProjectA)], Tags());
    }

    [Fact]
    public async Task DeleteRemovesTheTag()
    {
        // Arrange
        await TagAsync(Minutes(0, 60), ProjectA);

        // Act
        await _tagger.DeleteAsync(_store.Entries.Single(), undoDescription: "delete");

        // Assert
        Assert.Empty(_store.Entries);
    }

    [Fact]
    public async Task UndoDeleteBringsTheTagBack()
    {
        // Arrange
        await _tagger.TagAsync(Minutes(0, 60), ProjectA, taskId: null, note: "notes", undoDescription: "tag");
        var entry = _store.Entries.Single();
        await _tagger.DeleteAsync(entry, undoDescription: "delete");

        // Act
        await _tagger.UndoAsync();

        // Assert
        var restored = Assert.Single(_store.Entries);
        Assert.Equal((entry.Range, entry.ProjectId, entry.Note), (restored.Range, restored.ProjectId, restored.Note));
    }

    [Fact]
    public async Task RetagKeepsTheTime()
    {
        // Arrange
        await TagAsync(Minutes(0, 60), ProjectA);

        // Act
        await _tagger.RetagAsync(_store.Entries.Single(), ProjectB, TaskId, note: "note", undoDescription: "retag");

        // Assert
        var entry = Assert.Single(_store.Entries);
        Assert.Equal((Minutes(0, 60), ProjectB, (long?)TaskId, "note"), (entry.Range, entry.ProjectId, entry.TaskId, entry.Note));
    }

    [Fact]
    public async Task ResizeMovesTheEdge()
    {
        // Arrange
        await TagAsync(Minutes(0, 30), ProjectA);
        await TagAsync(Minutes(30, 90), ProjectB);

        // Act
        await _tagger.ResizeAsync(_store.Entries.Single(e => e.ProjectId == ProjectA), Minutes(0, 45), undoDescription: "resize");

        // Assert
        Assert.Equal([(Minutes(0, 45), ProjectA), (Minutes(45, 90), ProjectB)], Tags());
    }

    private static TimeRange Minutes(int from, int to) => new(Nine.AddMinutes(from), Nine.AddMinutes(to));

    private Task TagAsync(TimeRange range, long project, string undoDescription = "tag") =>
        _tagger.TagAsync(range, project, taskId: null, note: null, undoDescription);

    private List<(TimeRange Range, long Project)> Tags() =>
        [.. _store.Entries.OrderBy(e => e.Range.Start).Select(e => (e.Range, e.ProjectId))];

    /// <summary>
    /// Stores entries in memory, giving added ones increasing ids.
    /// </summary>
    private sealed class InMemoryTimeEntries : ITimeEntryRepository
    {
        private long _nextId = 1;

        public IReadOnlyList<TimeEntry> Entries { get; private set; } = [];

        // Touching entries don't overlap, as in the real repository.
        public Task<IReadOnlyList<TimeEntry>> GetAsync(TimeRange range, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TimeEntry>>([.. Entries.Where(e => e.Range.Overlaps(range)).OrderBy(e => e.Range.Start)]);

        public Task<IReadOnlyList<TimeEntry>> GetOverlappingOrTouchingAsync(TimeRange range) =>
            Task.FromResult<IReadOnlyList<TimeEntry>>([.. Entries
                .Where(e => e.Range.Start <= range.End && e.Range.End >= range.Start)
                .OrderBy(e => e.Range.Start)]);

        public Task<TimeEntryChanges> ApplyAsync(TimeEntryChanges changes)
        {
            var saved = changes with { Added = [.. changes.Added.Select(e => e with { Id = _nextId++ })] };
            Entries = saved.ApplyTo(Entries);
            return Task.FromResult(saved);
        }

        public Task<IReadOnlyList<(long ProjectId, long? TaskId)>> GetRecentTagsAsync(int count) =>
            Task.FromResult<IReadOnlyList<(long, long?)>>([]);
    }
}
