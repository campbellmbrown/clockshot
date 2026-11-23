using ClockShot.Core.Models;
using ClockShot.Core.Tagging;
using ClockShot.Data.Projects;
using ClockShot.Data.Tagging;

namespace ClockShot.Data.Tests;

public sealed class TaggerTests : IAsyncLifetime
{
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly MemoryDatabase _data = new();
    private readonly ProjectRepository _projects;
    private readonly TimeEntryRepository _entries;
    private readonly Tagger _tagger;
    private long _projectA;
    private long _projectB;
    private long _taskOfProjectB;

    public TaggerTests()
    {
        _projects = new ProjectRepository(_data.Database);
        _entries = new TimeEntryRepository(_data.Database);
        _tagger = new Tagger(_entries);
    }

    public async ValueTask InitializeAsync()
    {
        _projectA = (await _projects.AddProjectAsync("Project A", clientId: null, parentId: null)).Id;
        _projectB = (await _projects.AddProjectAsync("Project B", clientId: null, parentId: null)).Id;
        _taskOfProjectB = (await _projects.AddTaskAsync(_projectB, "Checkout")).Id;
    }

    public ValueTask DisposeAsync()
    {
        _data.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task TagReplacesOverlappingTagsAndAnnouncesTheChange()
    {
        // Arrange
        var changes = 0;
        _tagger.Changed += () => changes++;
        await TagAsync(Minutes(0, 60), _projectA);

        // Act
        await TagAsync(Minutes(30, 90), _projectB);

        // Assert
        Assert.Equal([(Minutes(0, 30), _projectA), (Minutes(30, 90), _projectB)], await TagsAsync());
        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task TagJoinsATouchingTagWithTheSameProject()
    {
        // Arrange
        await TagAsync(Minutes(0, 30), _projectA);

        // Act
        await TagAsync(Minutes(30, 60), _projectA);

        // Assert
        Assert.Equal([(Minutes(0, 60), _projectA)], await TagsAsync());
    }

    [Fact]
    public async Task UndoDescriptionSaysWhatUndoWillUndo()
    {
        // Act
        await TagAsync(Minutes(0, 60), _projectA, undoDescription: "Undo tagging 09:00 - 10:00");

        // Assert
        Assert.Equal("Undo tagging 09:00 - 10:00", _tagger.UndoDescription);
    }

    [Fact]
    public async Task UndoRestoresWhatWasThere()
    {
        // Arrange
        await _tagger.TagAsync(Minutes(0, 60), _projectA, taskId: null, note: "first", undoDescription: "tag");
        await TagAsync(Minutes(15, 45), _projectB);

        // Act
        await _tagger.UndoAsync();

        // Assert
        var entry = await SingleEntryAsync();
        Assert.Equal((Minutes(0, 60), _projectA, "first"), (entry.Range, entry.ProjectId, entry.Note));
        Assert.False(_tagger.CanUndo);
    }

    [Fact]
    public async Task ASecondUndoChangesNothing()
    {
        // Arrange
        await TagAsync(Minutes(0, 60), _projectA);
        await TagAsync(Minutes(15, 45), _projectB);
        await _tagger.UndoAsync();

        // Act
        await _tagger.UndoAsync();

        // Assert
        Assert.Equal([(Minutes(0, 60), _projectA)], await TagsAsync());
    }

    [Fact]
    public async Task DeleteRemovesTheTag()
    {
        // Arrange
        await TagAsync(Minutes(0, 60), _projectA);

        // Act
        await _tagger.DeleteAsync(await SingleEntryAsync(), undoDescription: "delete");

        // Assert
        Assert.Empty(await EntriesAsync());
    }

    [Fact]
    public async Task UndoDeleteBringsTheTagBack()
    {
        // Arrange
        await _tagger.TagAsync(Minutes(0, 60), _projectA, taskId: null, note: "notes", undoDescription: "tag");
        var entry = await SingleEntryAsync();
        await _tagger.DeleteAsync(entry, undoDescription: "delete");

        // Act
        await _tagger.UndoAsync();

        // Assert
        var restored = await SingleEntryAsync();
        Assert.Equal((entry.Range, entry.ProjectId, entry.Note), (restored.Range, restored.ProjectId, restored.Note));
    }

    [Fact]
    public async Task RetagKeepsTheTime()
    {
        // Arrange
        await TagAsync(Minutes(0, 60), _projectA);

        // Act
        await _tagger.RetagAsync(await SingleEntryAsync(), _projectB, _taskOfProjectB, note: "note", undoDescription: "retag");

        // Assert
        var entry = await SingleEntryAsync();
        Assert.Equal(
            (Minutes(0, 60), _projectB, (long?)_taskOfProjectB, "note"),
            (entry.Range, entry.ProjectId, entry.TaskId, entry.Note));
    }

    [Fact]
    public async Task RetagWithNothingChangedKeepsTheUndoAndAnnouncesNothing()
    {
        // Arrange
        await TagAsync(Minutes(0, 60), _projectA);
        var entry = await SingleEntryAsync();
        var undoBefore = _tagger.UndoDescription;
        var changes = 0;
        _tagger.Changed += () => changes++;

        // Act
        await _tagger.RetagAsync(entry, entry.ProjectId, entry.TaskId, entry.Note, undoDescription: "retag");

        // Assert
        Assert.Equal(undoBefore, _tagger.UndoDescription);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task ResizeMovesTheEdge()
    {
        // Arrange
        await TagAsync(Minutes(0, 30), _projectA);
        await TagAsync(Minutes(30, 90), _projectB);
        var projectAEntry = (await EntriesAsync()).Single(e => e.ProjectId == _projectA);

        // Act
        await _tagger.ResizeAsync(projectAEntry, Minutes(0, 45), undoDescription: "resize");

        // Assert
        Assert.Equal([(Minutes(0, 45), _projectA), (Minutes(45, 90), _projectB)], await TagsAsync());
    }

    private static TimeRange Minutes(int from, int to) => new(Nine.AddMinutes(from), Nine.AddMinutes(to));

    private Task TagAsync(TimeRange range, long project, string undoDescription = "tag") =>
        _tagger.TagAsync(range, project, taskId: null, note: null, undoDescription);

    /// <summary>
    /// Every entry the tests can make, which all fall within the first two hours.
    /// </summary>
    private Task<IReadOnlyList<TimeEntry>> EntriesAsync() =>
        _entries.GetAsync(Minutes(0, 120), TestContext.Current.CancellationToken);

    private async Task<TimeEntry> SingleEntryAsync() => Assert.Single(await EntriesAsync());

    private async Task<List<(TimeRange Range, long Project)>> TagsAsync() =>
        [.. (await EntriesAsync()).Select(e => (e.Range, e.ProjectId))];
}
