using System.IO.Abstractions.TestingHelpers;

using ClockShot.App.Services;
using ClockShot.App.ViewModels;
using ClockShot.App.ViewModels.Projects;
using ClockShot.Core.Models;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Data;
using ClockShot.Data.Projects;
using ClockShot.Data.Settings;
using ClockShot.Data.Tagging;
using ClockShot.Data.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

namespace ClockShot.App.Tests.ViewModels.Projects;

public sealed class ProjectsViewModelTests : IDisposable
{
    private static readonly DateTime NineAm = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    // Noon on the same day, so the tab's This week totals include the tagged time and show its General row.
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

    private readonly MemoryDatabase _database = new();
    private readonly ProjectRepository _projects;
    private readonly TimeEntryRepository _entries;
    private readonly Tagger _tagger;
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly JsonSettingsStore _settings = new(new MockFileSystem(), new DataPaths("data"), NullLogger<JsonSettingsStore>.Instance);

    public ProjectsViewModelTests()
    {
        _projects = new ProjectRepository(_database.Database);
        _entries = new TimeEntryRepository(_database.Database);
        _tagger = new Tagger(_entries);
    }

    [Theory]
    [InlineData("Login page")]
    [InlineData("General")]
    public async Task NewProjectWithATaskOrGeneralRowSelectedStartsInsideItsProject(string rowName)
    {
        // Arrange
        var website = await AddProjectWithTaggedTimeAsync();
        await _projects.AddTaskAsync(website.Id, "Login page");
        using var tab = await LoadedTabAsync();
        ProjectDialogViewModel? form = null;
        _dialogs.When(dialogs => dialogs.ShowAsync(Arg.Any<DialogViewModel>()))
            .Do(call => form = call.Arg<DialogViewModel>() as ProjectDialogViewModel);

        // Act
        await tab.NewProjectCommand.ExecuteAsync(Row(tab, rowName));

        // Assert
        Assert.Equal(website.Id, form?.SelectedParent?.Project?.Id);
    }

    [Fact]
    public async Task DeletingAProjectWithTaggedTimeSuggestsArchivingIt()
    {
        // Arrange
        await AddProjectWithTaggedTimeAsync();
        using var tab = await LoadedTabAsync();

        // Act
        await tab.DeleteCommand.ExecuteAsync(Row(tab, "Website"));

        // Assert
        Assert.Contains("Archive it instead", tab.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ArchivingClearsTheMessage()
    {
        // Arrange
        await AddProjectWithTaggedTimeAsync();
        using var tab = await LoadedTabAsync();
        await tab.DeleteCommand.ExecuteAsync(Row(tab, "Website"));

        // Act
        await tab.ToggleArchivedCommand.ExecuteAsync(Row(tab, "Website"));

        // Assert
        Assert.Equal("", tab.Message);
    }

    public void Dispose() => _database.Dispose();

    /// <summary>
    /// The tab's row with the given name, at any depth.
    /// </summary>
    private static CatalogNodeViewModel Row(ProjectsViewModel tab, string name) =>
        tab.Nodes.SelectMany(AndDescendants).First(row => row.Name == name);

    private static IEnumerable<CatalogNodeViewModel> AndDescendants(CatalogNodeViewModel row) =>
        row.Children.SelectMany(AndDescendants).Prepend(row);

    private async Task<Project> AddProjectWithTaggedTimeAsync()
    {
        var website = await _projects.AddProjectAsync("Website", clientId: null, parentId: null);
        await _tagger.TagAsync(new TimeRange(NineAm, NineAm.AddHours(1)), website.Id, taskId: null, note: null, undoDescription: "tag");
        return website;
    }

    private async Task<ProjectsViewModel> LoadedTabAsync()
    {
        var tab = new ProjectsViewModel(
            _projects,
            _entries,
            _tagger,
            _dialogs,
            _settings,
            new DayClock(_clock, _settings),
            new BackgroundWork(NullLogger<BackgroundWork>.Instance));
        await tab.LoadAsync();
        return tab;
    }
}
