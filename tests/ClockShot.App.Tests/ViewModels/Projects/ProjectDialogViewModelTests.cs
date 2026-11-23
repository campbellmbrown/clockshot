using ClockShot.App.ViewModels.Projects;
using ClockShot.Data.Projects;
using ClockShot.Data.Tests;

namespace ClockShot.App.Tests.ViewModels.Projects;

public sealed class ProjectDialogViewModelTests : IAsyncLifetime
{
    private readonly MemoryDatabase _database = new();
    private readonly ProjectRepository _projects;

    public ProjectDialogViewModelTests()
    {
        _projects = new ProjectRepository(_database.Database);
    }

    public async ValueTask InitializeAsync() =>
        await _projects.AddProjectAsync("Website", clientId: null, parentId: null);

    public ValueTask DisposeAsync()
    {
        _database.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SavingANameThatIsTakenShowsAnError()
    {
        // Arrange
        var form = await FormForNewProjectAsync(name: "Website");

        // Act
        await form.SaveCommand.ExecuteAsync(parameter: null);

        // Assert
        Assert.NotEqual("", form.Error);
    }

    [Fact]
    public async Task EditingTheNameClearsTheError()
    {
        // Arrange
        var form = await FormForNewProjectAsync(name: "Website");
        await form.SaveCommand.ExecuteAsync(parameter: null);

        // Act
        form.Name = "Website redesign";

        // Assert
        Assert.Equal("", form.Error);
    }

    [Fact]
    public async Task EditingTheClientClearsTheError()
    {
        // Arrange
        var form = await FormForNewProjectAsync(name: "Website");
        await form.SaveCommand.ExecuteAsync(parameter: null);

        // Act
        form.ClientText = "Client A";

        // Assert
        Assert.Equal("", form.Error);
    }

    [Fact]
    public async Task ChoosingAnotherParentClearsTheError()
    {
        // Arrange
        var form = await FormForNewProjectAsync(name: "Website");
        await form.SaveCommand.ExecuteAsync(parameter: null);

        // Act
        form.SelectedParent = form.ParentOptions.Single(option => option.Project?.Name == "Website");

        // Assert
        Assert.Equal("", form.Error);
    }

    private async Task<ProjectDialogViewModel> FormForNewProjectAsync(string name) =>
        ProjectDialogViewModel.ForNew(_projects, await _projects.LoadCatalogAsync(), name);
}
