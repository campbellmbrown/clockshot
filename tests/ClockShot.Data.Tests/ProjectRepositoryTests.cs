using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Tagging;
using ClockShot.Data.Projects;
using ClockShot.Data.Tagging;

namespace ClockShot.Data.Tests;

public sealed class ProjectRepositoryTests : IDisposable
{
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly MemoryDatabase _data = new();
    private readonly ProjectRepository _projects;
    private readonly TimeEntryRepository _entries;

    public ProjectRepositoryTests()
    {
        _projects = new ProjectRepository(_data.Database);
        _entries = new TimeEntryRepository(_data.Database);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => _data.Dispose();

    [Fact]
    public async Task AddsClientsProjectsSubProjectsAndTasks()
    {
        // Arrange
        var changes = 0;
        _projects.Changed += () => changes++;

        // Act
        var client = await _projects.AddClientAsync("Client A");
        var website = await _projects.AddProjectAsync("Website", client.Id, parentId: null);
        var checkout = await _projects.AddProjectAsync("Checkout", client.Id, website.Id);
        await _projects.AddTaskAsync(checkout.Id, "Payments");
        var catalog = await _projects.LoadCatalogAsync(CancellationToken);

        // Assert
        Assert.Null(checkout.ClientId);
        Assert.Equal("Client A > Website > Checkout", catalog.PathOf(catalog.ProjectById(checkout.Id)!));
        Assert.Equal(["Payments"], catalog.TasksOf(checkout.Id).Select(t => t.Name));
        Assert.Equal(4, changes);
    }

    [Fact]
    public async Task NamesAreSavedWithoutSurroundingSpaces()
    {
        // Act
        var client = await _projects.AddClientAsync("  Client A  ");
        var project = await _projects.AddProjectAsync("  Website  ", client.Id, parentId: null);
        var task = await _projects.AddTaskAsync(project.Id, "  Login page  ");

        // Assert
        var catalog = await _projects.LoadCatalogAsync(CancellationToken);
        Assert.Equal("Client A", catalog.ClientById(client.Id)?.Name);
        Assert.Equal("Website", catalog.ProjectById(project.Id)?.Name);
        Assert.Equal("Login page", catalog.TaskById(task.Id)?.Name);
    }

    [Fact]
    public async Task ASubProjectCanShareItsParentsName()
    {
        // Arrange
        var website = await AddTopLevelProjectAsync("Website");

        // Act
        var subProject = await _projects.AddProjectAsync("Website", clientId: null, website.Id);

        // Assert
        Assert.Equal(website.Id, subProject.ParentId);
    }

    [Fact]
    public async Task ProjectNamesThatDifferOnlyInCaseAreRefused()
    {
        // Arrange
        await AddTopLevelProjectAsync("Website");

        // Act
        var adding = AddTopLevelProjectAsync("WEBSITE");

        // Assert
        await Assert.ThrowsAsync<DuplicateNameException>(() => adding);
    }

    [Fact]
    public async Task TaskNamesThatDifferOnlyInCaseAreRefused()
    {
        // Arrange
        var website = await AddTopLevelProjectAsync("Website");
        await _projects.AddTaskAsync(website.Id, "Design");

        // Act
        var adding = _projects.AddTaskAsync(website.Id, "design");

        // Assert
        await Assert.ThrowsAsync<DuplicateNameException>(() => adding);
    }

    [Fact]
    public async Task OtherConstraintFailuresAreNotReportedAsDuplicateNames()
    {
        // Act
        var error = await Assert.ThrowsAnyAsync<Exception>(() => _projects.AddTaskAsync(projectId: 999, "Design"));

        // Assert
        Assert.IsNotType<DuplicateNameException>(error);
    }

    [Fact]
    public async Task DeleteOnlyRemovesUnusedItems()
    {
        // Arrange
        var used = await AddTopLevelProjectAsync("Used");
        var unused = await AddTopLevelProjectAsync("Unused");
        await _projects.AddTaskAsync(unused.Id, "Never used task");
        await AddEntriesAsync(used.Id, Minutes(0, 30));

        // Act
        var deletedUsed = await _projects.DeleteIfUnusedAsync(new CatalogItem(CatalogItemKind.Project, used.Id));
        var deletedUnused = await _projects.DeleteIfUnusedAsync(new CatalogItem(CatalogItemKind.Project, unused.Id));

        // Assert
        var catalog = await _projects.LoadCatalogAsync(CancellationToken);
        Assert.False(deletedUsed);
        Assert.True(deletedUnused);
        Assert.Equal(["Used"], catalog.Projects.Select(p => p.Name));
        Assert.Empty(catalog.Tasks);
    }

    [Fact]
    public async Task DeletingAProjectRemovesItsTagColours()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        await _projects.SetTagColorAsync(project.Id, taskId: null, colorHex: "#123456");

        // Act
        var deleted = await _projects.DeleteIfUnusedAsync(new CatalogItem(CatalogItemKind.Project, project.Id));

        // Assert
        var catalog = await _projects.LoadCatalogAsync(CancellationToken);
        Assert.True(deleted);
        Assert.Null(catalog.ChosenColorOf(project.Id, taskId: null));
    }

    [Fact]
    public async Task DeletingAnItemThatIsAlreadyGoneSucceeds()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var item = new CatalogItem(CatalogItemKind.Project, project.Id);
        await _projects.DeleteIfUnusedAsync(item);

        // Act
        var deleted = await _projects.DeleteIfUnusedAsync(item);

        // Assert
        Assert.True(deleted);
    }

    [Fact]
    public async Task ArchivingAnnouncesTheChange()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var changes = 0;
        _projects.Changed += () => changes++;

        // Act
        await _projects.SetArchivedAsync(new CatalogItem(CatalogItemKind.Project, project.Id), archived: true);

        // Assert
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task ARefusedDeleteDoesNotAnnounceAChange()
    {
        // Arrange
        var used = await AddTopLevelProjectAsync("Used");
        await AddEntriesAsync(used.Id, Minutes(0, 30));
        var changes = 0;
        _projects.Changed += () => changes++;

        // Act
        await _projects.DeleteIfUnusedAsync(new CatalogItem(CatalogItemKind.Project, used.Id));

        // Assert
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task ClearingAColourThatWasNeverChosenDoesNotAnnounceAChange()
    {
        // Arrange
        var website = await AddTopLevelProjectAsync("Website");
        var changes = 0;
        _projects.Changed += () => changes++;

        // Act
        await _projects.SetTagColorAsync(website.Id, taskId: null, colorHex: null);

        // Assert
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task ProjectsCanBeRenamed()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Old name");

        // Act
        await _projects.RenameAsync(new CatalogItem(CatalogItemKind.Project, project.Id), "New name");

        // Assert
        var saved = (await _projects.LoadCatalogAsync(CancellationToken)).ProjectById(project.Id)!;
        Assert.Equal("New name", saved.Name);
    }

    [Fact]
    public async Task ClientsCanBeRenamed()
    {
        // Arrange
        var client = await _projects.AddClientAsync("Old name");

        // Act
        await _projects.RenameAsync(new CatalogItem(CatalogItemKind.Client, client.Id), "New name");

        // Assert
        var saved = (await _projects.LoadCatalogAsync(CancellationToken)).Clients.Single();
        Assert.Equal("New name", saved.Name);
    }

    [Fact]
    public async Task TasksCanBeRenamed()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var task = await _projects.AddTaskAsync(project.Id, "Old name");

        // Act
        await _projects.RenameAsync(new CatalogItem(CatalogItemKind.Task, task.Id), "New name");

        // Assert
        var saved = (await _projects.LoadCatalogAsync(CancellationToken)).Tasks.Single();
        Assert.Equal("New name", saved.Name);
    }

    [Fact]
    public async Task RenamingToATakenNameIsRefused()
    {
        // Arrange
        await _projects.AddClientAsync("Client A");
        var clientB = await _projects.AddClientAsync("Client B");

        // Act
        var exception = await Record.ExceptionAsync(
            () => _projects.RenameAsync(new CatalogItem(CatalogItemKind.Client, clientB.Id), "client a"));

        // Assert
        Assert.IsType<DuplicateNameException>(exception);
    }

    [Fact]
    public async Task ProjectsCanBeArchived()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");

        // Act
        await _projects.SetArchivedAsync(new CatalogItem(CatalogItemKind.Project, project.Id), archived: true);

        // Assert
        var saved = (await _projects.LoadCatalogAsync(CancellationToken)).ProjectById(project.Id)!;
        Assert.True(saved.IsArchived);
    }

    [Fact]
    public async Task ClientsCanBeArchived()
    {
        // Arrange
        var client = await _projects.AddClientAsync("Client A");

        // Act
        await _projects.SetArchivedAsync(new CatalogItem(CatalogItemKind.Client, client.Id), archived: true);

        // Assert
        var saved = (await _projects.LoadCatalogAsync(CancellationToken)).Clients.Single();
        Assert.True(saved.IsArchived);
    }

    [Fact]
    public async Task TasksCanBeArchived()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var task = await _projects.AddTaskAsync(project.Id, "Login page");

        // Act
        await _projects.SetArchivedAsync(new CatalogItem(CatalogItemKind.Task, task.Id), archived: true);

        // Assert
        var saved = (await _projects.LoadCatalogAsync(CancellationToken)).Tasks.Single();
        Assert.True(saved.IsArchived);
    }

    [Fact]
    public async Task UnusedClientsCanBeDeleted()
    {
        // Arrange
        var client = await _projects.AddClientAsync("Client A");

        // Act
        var deleted = await _projects.DeleteIfUnusedAsync(new CatalogItem(CatalogItemKind.Client, client.Id));

        // Assert
        Assert.True(deleted);
        Assert.Empty((await _projects.LoadCatalogAsync(CancellationToken)).Clients);
    }

    [Fact]
    public async Task UnusedTasksCanBeDeleted()
    {
        // Arrange
        var project = await AddTopLevelProjectAsync("Website");
        var task = await _projects.AddTaskAsync(project.Id, "Login page");

        // Act
        var deleted = await _projects.DeleteIfUnusedAsync(new CatalogItem(CatalogItemKind.Task, task.Id));

        // Assert
        Assert.True(deleted);
        Assert.Empty((await _projects.LoadCatalogAsync(CancellationToken)).Tasks);
    }

    [Fact]
    public async Task TagColoursAreSavedPerProjectAndTask()
    {
        // Arrange
        var website = await AddTopLevelProjectAsync("Website");
        var login = await _projects.AddTaskAsync(website.Id, "Login page");

        // Act
        await _projects.SetTagColorAsync(website.Id, login.Id, "#C0453F");
        await _projects.SetTagColorAsync(website.Id, taskId: null, "#3B82C4");

        // Assert
        var catalog = await _projects.LoadCatalogAsync(CancellationToken);
        Assert.Equal("#C0453F", catalog.ColorOf(website.Id, login.Id));
        Assert.Equal("#3B82C4", catalog.ColorOf(website.Id, taskId: null));
    }

    [Fact]
    public async Task ChoosingATagColourAgainReplacesIt()
    {
        // Arrange
        var website = await AddTopLevelProjectAsync("Website");
        await _projects.SetTagColorAsync(website.Id, taskId: null, "#3B82C4");

        // Act
        await _projects.SetTagColorAsync(website.Id, taskId: null, "#2E9E8F");

        // Assert
        var catalog = await _projects.LoadCatalogAsync(CancellationToken);
        Assert.Equal("#2E9E8F", catalog.ColorOf(website.Id, taskId: null));
    }

    [Fact]
    public async Task ClearingATagColourGoesBackToTheAutomaticOne()
    {
        // Arrange
        var website = await AddTopLevelProjectAsync("Website");
        var login = await _projects.AddTaskAsync(website.Id, "Login page");
        await _projects.SetTagColorAsync(website.Id, login.Id, "#C0453F");

        // Act
        await _projects.SetTagColorAsync(website.Id, login.Id, colorHex: null);

        // Assert
        var catalog = await _projects.LoadCatalogAsync(CancellationToken);
        Assert.Null(catalog.ChosenColorOf(website.Id, login.Id));
        Assert.Equal(NameColor.For("Website > Login page"), catalog.ColorOf(website.Id, login.Id));
    }

    [Fact]
    public async Task UpdateMovesAndRenamesAProject()
    {
        // Arrange
        var client = await _projects.AddClientAsync("Client A");
        var website = await _projects.AddProjectAsync("Website", client.Id, parentId: null);
        var checkout = await _projects.AddProjectAsync("Checkout", clientId: null, website.Id);

        // Act
        await _projects.UpdateProjectAsync(checkout with { ParentId = null, ClientId = null, Name = "Shop" });

        // Assert
        var moved = (await _projects.LoadCatalogAsync(CancellationToken)).ProjectById(checkout.Id)!;
        Assert.Equal((null, null, "Shop"), (moved.ParentId, moved.ClientId, moved.Name));
    }

    [Fact]
    public async Task MovingAProjectUnderAnotherGivesItThatProjectsClient()
    {
        // Arrange
        var client = await _projects.AddClientAsync("Client A");
        var website = await _projects.AddProjectAsync("Website", client.Id, parentId: null);
        var shop = await AddTopLevelProjectAsync("Shop");

        // Act
        await _projects.UpdateProjectAsync(shop with { ParentId = website.Id });

        // Assert
        var catalog = await _projects.LoadCatalogAsync(CancellationToken);
        var moved = catalog.ProjectById(shop.Id)!;
        Assert.Equal((website.Id, null), (moved.ParentId, moved.ClientId));
        Assert.Equal("Client A", catalog.ClientOf(moved)?.Name);
    }

    [Fact]
    public async Task UpdateRefusesToMoveAProjectInsideItsOwnSubProject()
    {
        // Arrange
        var website = await AddTopLevelProjectAsync("Website");
        var checkout = await _projects.AddProjectAsync("Checkout", clientId: null, website.Id);

        // Act
        var moving = _projects.UpdateProjectAsync(website with { ParentId = checkout.Id });

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(() => moving);
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
