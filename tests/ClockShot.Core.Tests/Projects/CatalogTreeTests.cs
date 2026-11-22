using ClockShot.Core.Models;
using ClockShot.Core.Projects;

namespace ClockShot.Core.Tests.Projects;

public sealed class CatalogTreeTests
{
    private const long ClientAId = 1;
    private const long ClientBId = 2;
    private const long WebsiteId = 10;
    private const long CheckoutId = 11;
    private const long InternalId = 12;
    private const long LoginPageId = 100;
    private const long ArchivedTaskId = 101;

    private static readonly ProjectCatalog Catalog = new(
        [
            new Client(ClientBId, "Client B", IsArchived: true),
            new Client(ClientAId, "Client A", IsArchived: false),
        ],
        [
            new Project(WebsiteId, ClientAId, ParentId: null, "Website", IsArchived: false),
            new Project(CheckoutId, ClientId: null, WebsiteId, "Checkout", IsArchived: true),
            new Project(InternalId, ClientId: null, ParentId: null, "Internal", IsArchived: false),
        ],
        [
            new ProjectTask(LoginPageId, WebsiteId, "Login page", IsArchived: false),
            new ProjectTask(ArchivedTaskId, WebsiteId, "Archived task", IsArchived: true),
        ]);

    [Fact]
    public void ClientsComeByNameThenTheNoClientGroup()
    {
        // Act
        var tree = CatalogTree.Build(Catalog);

        // Assert
        Assert.Equal(["Client A", "Client B", "No client"], tree.Select(n => n.Name));
        Assert.Equal([CatalogRowKind.Client, CatalogRowKind.Client, CatalogRowKind.NoClient], tree.Select(n => n.Kind));
        Assert.Equal(["Internal"], tree[2].Children.Select(n => n.Name));
    }

    [Fact]
    public void ProjectsHoldTheirSubProjectsThenGeneralThenTheirTasks()
    {
        // Act
        var website = Assert.Single(CatalogTree.Build(Catalog)[0].Children);

        // Assert
        Assert.Equal(["Checkout", "General", "Archived task", "Login page"], website.Children.Select(n => n.Name));
        Assert.Equal(
            [CatalogRowKind.Project, CatalogRowKind.General, CatalogRowKind.Task, CatalogRowKind.Task],
            website.Children.Select(n => n.Kind));
        Assert.Equal(["General"], Row(website, "Checkout").Children.Select(n => n.Name));
    }

    [Fact]
    public void ArchivedItemsAreIncludedAndMarked()
    {
        // Act
        var tree = CatalogTree.Build(Catalog);

        // Assert
        var website = Row(tree[0], "Website");
        Assert.True(tree.Single(n => n.Name == "Client B").IsArchived);
        Assert.True(Row(website, "Checkout").IsArchived);
        Assert.True(Row(website, "Archived task").IsArchived);
        Assert.False(Row(website, "Login page").IsArchived);
    }

    [Fact]
    public void OnlyTaskAndGeneralRowsHaveATag()
    {
        // Act
        var tree = CatalogTree.Build(Catalog);

        // Assert
        var website = Row(tree[0], "Website");
        Assert.Equal((WebsiteId, (long?)null), Row(website, "General").Tag);
        Assert.Equal((WebsiteId, (long?)LoginPageId), Row(website, "Login page").Tag);
        Assert.Null(website.Tag);
        Assert.Null(tree[0].Tag);
    }

    [Fact]
    public void ClientProjectAndTaskRowsNameTheirItem()
    {
        // Act
        var tree = CatalogTree.Build(Catalog);

        // Assert
        var website = Row(tree[0], "Website");
        Assert.Equal(new CatalogItem(CatalogItemKind.Client, ClientAId), tree[0].Item);
        Assert.Equal(new CatalogItem(CatalogItemKind.Project, WebsiteId), website.Item);
        Assert.Equal(new CatalogItem(CatalogItemKind.Task, LoginPageId), Row(website, "Login page").Item);
        Assert.Null(Row(website, "General").Item);
        Assert.Null(tree[2].Item);
    }

    [Fact]
    public void AnEmptyCatalogHasOnlyAnEmptyNoClientGroup()
    {
        // Act
        var tree = CatalogTree.Build(ProjectCatalog.Empty);

        // Assert
        var noClient = Assert.Single(tree);
        Assert.Equal(CatalogRowKind.NoClient, noClient.Kind);
        Assert.Empty(noClient.Children);
    }

    private static CatalogNode Row(CatalogNode parent, string name) => parent.Children.Single(n => n.Name == name);
}
