using ClockShot.Core.Models;
using ClockShot.Core.Projects;

namespace ClockShot.Core.Tests.Projects;

public sealed class ProjectCatalogTests
{
    private const long ClientAId = 1;
    private const long ArchivedClientId = 2;
    private const long WebsiteId = 10;
    private const long CheckoutId = 11;
    private const long InternalId = 12;
    private const long OldAppId = 13;
    private const long WebShopId = 14;
    private const long AccountId = 15;
    private const long LoginPageId = 100;
    private const long ArchivedTaskId = 101;

    private static readonly ProjectCatalog Catalog = new(
        [
            new Client(ClientAId, "Client A", IsArchived: false),
            new Client(ArchivedClientId, "Client C", IsArchived: true),
        ],
        [
            new Project(WebsiteId, ClientAId, ParentId: null, "Website", IsArchived: false),
            new Project(CheckoutId, ClientId: null, WebsiteId, "Checkout", IsArchived: false),
            new Project(InternalId, ClientId: null, ParentId: null, "Internal", IsArchived: false),
            new Project(OldAppId, ArchivedClientId, ParentId: null, "Old app", IsArchived: false),
            new Project(WebShopId, ClientAId, ParentId: null, "Web shop", IsArchived: true),
        ],
        [
            new ProjectTask(LoginPageId, WebsiteId, "Login page", IsArchived: false),
            new ProjectTask(ArchivedTaskId, WebsiteId, "Archived task", IsArchived: true),
        ]);

    [Fact]
    public void SubProjectsUseTheirParentsClientAndShowTheirPath()
    {
        // Arrange
        var checkout = Catalog.ProjectById(CheckoutId)!;

        // Act
        var client = Catalog.ClientOf(checkout);
        var context = Catalog.ContextOf(checkout);
        var path = Catalog.PathOf(checkout);

        // Assert
        Assert.Equal("Client A", client?.Name);
        Assert.Equal("Client A > Website", context);
        Assert.Equal("Client A > Website > Checkout", path);
    }

    [Fact]
    public void NoIdFindsNoProject()
    {
        // Act
        var project = Catalog.ProjectById(id: null);

        // Assert
        Assert.Null(project);
    }

    [Fact]
    public void ATopLevelProjectWithoutAClientIsItsOwnPath()
    {
        // Act
        var path = Catalog.PathOf(Catalog.ProjectById(InternalId)!);

        // Assert
        Assert.Equal("Internal", path);
    }

    [Fact]
    public void TagLabelsShowProjectAndTask()
    {
        // Act
        var taskLabel = Catalog.TagLabel(WebsiteId, LoginPageId);
        var generalLabel = Catalog.TagLabel(WebsiteId, taskId: null);

        // Assert
        Assert.Equal("Website > Login page", taskLabel);
        Assert.Equal("Website", generalLabel);
    }

    [Fact]
    public void TagsWithoutAChosenColourGetTheColourOfTheirLabel()
    {
        // Act
        var general = Catalog.ColorOf(WebsiteId, taskId: null);
        var task = Catalog.ColorOf(WebsiteId, ArchivedTaskId);

        // Assert
        Assert.Equal(NameColor.For("Website"), general);
        Assert.Equal(NameColor.For("Website > Archived task"), task);
    }

    [Fact]
    public void LabelsFromNamesMatchLabelsFromIds()
    {
        // Act
        var taskLabel = ProjectCatalog.LabelOf("Website", "Login page");
        var blankTaskLabel = ProjectCatalog.LabelOf("Website", " ");

        // Assert
        Assert.Equal(Catalog.TagLabel(WebsiteId, LoginPageId), taskLabel);
        Assert.Equal(Catalog.TagLabel(WebsiteId, taskId: null), blankTaskLabel);
    }

    [Fact]
    public void UnarchivedProjectsLeaveOutArchivedProjectsAndArchivedClients()
    {
        // Act
        var projects = Catalog.UnarchivedProjects();

        // Assert
        Assert.DoesNotContain(projects, p => p.Name is "Old app" or "Web shop");
    }

    [Fact]
    public void UnarchivedProjectsAreInPathOrder()
    {
        // Arrange
        var catalog = new ProjectCatalog(
            [new Client(ClientAId, "Client A", IsArchived: false)],
            [
                new Project(InternalId, ClientId: null, ParentId: null, "Internal", IsArchived: false),
                new Project(CheckoutId, ClientId: null, WebsiteId, "Checkout", IsArchived: false),
                new Project(WebsiteId, ClientAId, ParentId: null, "Website", IsArchived: false),
            ],
            []);

        // Act
        var projects = catalog.UnarchivedProjects();

        // Assert
        Assert.Equal(["Website", "Checkout", "Internal"], projects.Select(p => p.Name));
    }

    [Fact]
    public void TaskNamedFindsATaskIgnoringCaseAndSpacesIncludingArchivedOnes()
    {
        // Act
        var login = Catalog.TaskNamed(WebsiteId, " login PAGE ");
        var archived = Catalog.TaskNamed(WebsiteId, "archived task");

        // Assert
        Assert.Equal(LoginPageId, login?.Id);
        Assert.Equal(ArchivedTaskId, archived?.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("New task")]
    public void TaskNamedIsNullForABlankOrNewName(string? name)
    {
        // Act
        var task = Catalog.TaskNamed(WebsiteId, name);

        // Assert
        Assert.Null(task);
    }

    [Fact]
    public void TaskNamedOnlyLooksInTheGivenProject()
    {
        // Act
        var task = Catalog.TaskNamed(InternalId, "Login page");

        // Assert
        Assert.Null(task);
    }

    [Fact]
    public void ClientNamedFindsAClientIgnoringCaseAndSpaces()
    {
        // Act
        var client = Catalog.ClientNamed(" client a ");

        // Assert
        Assert.Equal(ClientAId, client?.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("Client B")]
    public void ClientNamedIsNullForABlankOrNewName(string? name)
    {
        // Act
        var client = Catalog.ClientNamed(name);

        // Assert
        Assert.Null(client);
    }

    [Fact]
    public void ChildrenOfListsSubProjectsByName()
    {
        // Arrange
        var catalog = new ProjectCatalog(
            [],
            [
                new Project(WebsiteId, ClientId: null, ParentId: null, "Website", IsArchived: false),
                new Project(CheckoutId, ClientId: null, WebsiteId, "Checkout", IsArchived: false),
                new Project(AccountId, ClientId: null, WebsiteId, "Account", IsArchived: false),
            ],
            []);

        // Act
        var children = catalog.ChildrenOf(catalog.ProjectById(WebsiteId)!);

        // Assert
        Assert.Equal(["Account", "Checkout"], children.Select(p => p.Name));
    }

    [Fact]
    public void TopLevelProjectsOfAClientAreListedByName()
    {
        // Arrange
        var catalog = new ProjectCatalog(
            [new Client(ClientAId, "Client A", IsArchived: false)],
            [
                new Project(WebsiteId, ClientAId, ParentId: null, "Website", IsArchived: false),
                new Project(AccountId, ClientAId, ParentId: null, "Account", IsArchived: false),
                new Project(CheckoutId, ClientAId, WebsiteId, "Checkout", IsArchived: false),
                new Project(OldAppId, ArchivedClientId, ParentId: null, "Old app", IsArchived: false),
            ],
            []);

        // Act
        var projects = catalog.TopLevelProjectsOf(ClientAId);

        // Assert
        Assert.Equal(["Account", "Website"], projects.Select(p => p.Name));
    }

    [Fact]
    public void TopLevelProjectsOfNoClientAreTheOnesWithoutAClient()
    {
        // Act
        var projects = Catalog.TopLevelProjectsOf(clientId: null);

        // Assert
        Assert.Equal(["Internal"], projects.Select(p => p.Name));
    }

    [Fact]
    public void AProjectInsideAnArchivedProjectIsArchived()
    {
        // Arrange
        var catalog = new ProjectCatalog(
            [],
            [
                new Project(WebsiteId, ClientId: null, ParentId: null, "Website", IsArchived: true),
                new Project(CheckoutId, ClientId: null, WebsiteId, "Checkout", IsArchived: false),
            ],
            []);

        // Act
        var isArchived = catalog.IsArchived(catalog.ProjectById(CheckoutId)!);

        // Assert
        Assert.True(isArchived);
    }

    [Fact]
    public void AncestorsOfFinishesWhenBadDataMakesALoop()
    {
        // Arrange
        var catalog = new ProjectCatalog(
            [],
            [
                new Project(WebsiteId, ClientId: null, CheckoutId, "Website", IsArchived: false),
                new Project(CheckoutId, ClientId: null, WebsiteId, "Checkout", IsArchived: false),
            ],
            []);

        // Act
        // Without the depth limit, this never finishes.
        var ancestors = catalog.AncestorsOf(catalog.ProjectById(WebsiteId)!);

        // Assert
        Assert.NotEmpty(ancestors);
    }

    [Fact]
    public void TasksOfLeavesOutArchivedTasksUnlessAsked()
    {
        // Act
        var unarchived = Catalog.TasksOf(WebsiteId);
        var all = Catalog.TasksOf(WebsiteId, includeArchived: true);

        // Assert
        Assert.Equal(["Login page"], unarchived.Select(t => t.Name));
        Assert.Equal(2, all.Count);
    }
}
