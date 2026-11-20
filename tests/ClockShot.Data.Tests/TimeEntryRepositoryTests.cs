using ClockShot.Core.Models;
using ClockShot.Core.Tagging;
using ClockShot.Data.Projects;
using ClockShot.Data.Tagging;

using Microsoft.Data.Sqlite;

namespace ClockShot.Data.Tests;

public sealed class TimeEntryRepositoryTests : IDisposable
{
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly MemoryDatabase _data = new();
    private readonly ProjectRepository _projects;
    private readonly TimeEntryRepository _entries;

    public TimeEntryRepositoryTests()
    {
        _projects = new ProjectRepository(_data.Database);
        _entries = new TimeEntryRepository(_data.Database);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => _data.Dispose();

    [Fact]
    public async Task RecentTagsAreNewestFirst()
    {
        // Arrange
        var website = await AddTopLevelProjectAsync("Website");
        var shop = await AddTopLevelProjectAsync("Shop");
        await AddEntriesAsync(website.Id, Minutes(0, 15));
        await AddEntriesAsync(shop.Id, Minutes(15, 30));

        // Act
        var recent = await _entries.GetRecentTagsAsync(count: 5);

        // Assert
        Assert.Equal([(shop.Id, (long?)null), (website.Id, (long?)null)], recent);
    }

    [Fact]
    public async Task OverlappingOrTouchingIncludesEntriesThatTouchEitherEnd()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var oneMillisecond = TimeSpan.FromMilliseconds(1);
        await AddEntriesAsync(
            project.Id,
            Minutes(0, 30),
            Minutes(60, 90),
            new TimeRange(Nine.AddMinutes(90).Add(oneMillisecond), Nine.AddMinutes(120)));

        // Act
        var found = await _entries.GetOverlappingOrTouchingAsync(Minutes(30, 60));

        // Assert
        Assert.Equal([Minutes(0, 30), Minutes(60, 90)], found.Select(e => e.Range));
    }

    [Fact]
    public async Task GetLeavesOutEntriesThatOnlyTouchTheRange()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        await AddEntriesAsync(project.Id, Minutes(0, 30), Minutes(30, 60), Minutes(60, 90));

        // Act
        var found = await _entries.GetAsync(Minutes(30, 60), CancellationToken);

        // Assert
        Assert.Equal([Minutes(30, 60)], found.Select(e => e.Range));
    }

    [Fact]
    public async Task ApplySavesAddedUpdatedAndRemovedEntries()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var otherProject = await AddTopLevelProjectAsync("Shop");
        var task = await _projects.AddTaskAsync(otherProject.Id, "Checkout");
        var saved = (await AddEntriesAsync(project.Id, Minutes(0, 30), Minutes(30, 60))).Added;
        var updated = saved[0] with
        {
            Range = Minutes(0, 15),
            ProjectId = otherProject.Id,
            TaskId = task.Id,
            Note = "changed",
        };
        var added = new TimeEntry(TimeEntry.UnsavedId, Minutes(60, 90), project.Id, task.Id, Note: "new");

        // Act
        var applied = await _entries.ApplyAsync(new TimeEntryChanges(
            Added: [added],
            Updated: [(saved[0], updated)],
            Removed: [saved[1]]));

        // Assert
        var addedWithId = Assert.Single(applied.Added);
        Assert.NotEqual(TimeEntry.UnsavedId, addedWithId.Id);
        Assert.Equal([updated, addedWithId], await _entries.GetAsync(Minutes(0, 90), CancellationToken));
    }

    [Fact]
    public async Task AFailingChangeLeavesEverythingAsItWas()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var existing = Assert.Single((await AddEntriesAsync(project.Id, Minutes(0, 30))).Added);
        var entryForUnknownProject = new TimeEntry(
            TimeEntry.UnsavedId,
            Minutes(30, 60),
            ProjectId: 999,
            TaskId: null,
            Note: null);

        // Act
        var applying = _entries.ApplyAsync(new TimeEntryChanges(
            Added: [entryForUnknownProject],
            Updated: [],
            Removed: [existing]));

        // Assert
        await Assert.ThrowsAsync<SqliteException>(() => applying);
        Assert.Equal([existing], await _entries.GetAsync(Minutes(0, 60), CancellationToken));
    }

    [Fact]
    public async Task ApplyReturnsTimesAsTheyAreSaved()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var existing = Assert.Single((await AddEntriesAsync(project.Id, Minutes(0, 30))).Added);
        var finerThanAMillisecond = TimeSpan.FromTicks(1);
        var updated = existing with { Range = new TimeRange(Nine, Nine.AddMinutes(15).Add(finerThanAMillisecond)) };
        var added = existing with
        {
            Id = TimeEntry.UnsavedId,
            Range = new TimeRange(Nine.AddMinutes(30).Add(finerThanAMillisecond), Nine.AddMinutes(60)),
        };

        // Act
        var applied = await _entries.ApplyAsync(new TimeEntryChanges(
            Added: [added],
            Updated: [(existing, updated)],
            Removed: []));

        // Assert
        var stored = await _entries.GetAsync(Minutes(0, 60), CancellationToken);
        Assert.Equal([applied.Updated.Single().After, applied.Added.Single()], stored);
    }

    private static TimeRange Minutes(int from, int to) => new(Nine.AddMinutes(from), Nine.AddMinutes(to));

    private Task<Project> AddTopLevelProjectAsync(string name) =>
        _projects.AddProjectAsync(name, clientId: null, parentId: null);

    private Task<TimeEntryChanges> AddEntriesAsync(long projectId, params TimeRange[] ranges) =>
        _entries.ApplyAsync(new TimeEntryChanges(
            Added: [.. ranges.Select(r => new TimeEntry(TimeEntry.UnsavedId, r, projectId, TaskId: null, Note: null))],
            Updated: [],
            Removed: []));
}
