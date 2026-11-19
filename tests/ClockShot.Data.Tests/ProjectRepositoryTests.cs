using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Data.Projects;

namespace ClockShot.Data.Tests;

public sealed class ProjectRepositoryTests : IDisposable
{
    private readonly MemoryDatabase _data = new();
    private readonly ProjectRepository _projects;

    public ProjectRepositoryTests()
    {
        _projects = new ProjectRepository(_data.Database);
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

    private Task<Project> AddTopLevelProjectAsync(string name) =>
        _projects.AddProjectAsync(name, clientId: null, parentId: null);
}
