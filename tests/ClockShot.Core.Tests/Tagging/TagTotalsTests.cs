using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Tagging;

namespace ClockShot.Core.Tests.Tagging;

public sealed class TagTotalsTests
{
    private const long ClientId = 3;
    private const long ProjectA = 1;
    private const long ProjectB = 2;
    private const long TaskId = 7;
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TotalsSumTimePerProjectAndTaskInsideTheRange()
    {
        // Arrange
        TimeEntry[] entries =
        [
            new(Id: 1, Minutes(0, 30), ProjectA, TaskId: null, Note: null),
            new(Id: 2, Minutes(30, 40), ProjectA, TaskId, Note: null),
            new(Id: 3, Minutes(40, 90), ProjectB, TaskId: null, Note: null),
        ];

        // Act
        var totals = TagTotals.Of(entries, Minutes(0, 60));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), totals.ForTask(ProjectA, taskId: null));
        Assert.Equal(TimeSpan.FromMinutes(10), totals.ForTask(ProjectA, TaskId));
        Assert.Equal(TimeSpan.FromMinutes(20), totals.ForTask(ProjectB, taskId: null));
    }

    [Fact]
    public void TheTotalIsAllTaggedTimeInsideTheRange()
    {
        // Arrange
        TimeEntry[] entries =
        [
            new(Id: 1, Minutes(0, 30), ProjectA, TaskId: null, Note: null),
            new(Id: 2, Minutes(40, 90), ProjectB, TaskId: null, Note: null),
        ];

        // Act
        var total = TagTotals.Of(entries, Minutes(0, 60)).Total;

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(50), total);
    }

    [Fact]
    public void TotalOfARowIncludesEveryRowUnderItEvenArchived()
    {
        // Arrange
        var catalog = new ProjectCatalog(
            [new Client(ClientId, "Client A", IsArchived: false)],
            [
                new Project(ProjectA, ClientId, ParentId: null, "Website", IsArchived: false),
                new Project(ProjectB, ClientId: null, ProjectA, "Checkout", IsArchived: true),
            ],
            [new ProjectTask(TaskId, ProjectA, "Login page", IsArchived: true)]);
        var client = CatalogTree.Build(catalog)[0];
        var website = client.Children[0];
        var checkout = website.Children[0];
        TimeEntry[] entries =
        [
            new(Id: 1, Minutes(0, 30), ProjectA, TaskId: null, Note: null),
            new(Id: 2, Minutes(30, 40), ProjectA, TaskId, Note: null),
            new(Id: 3, Minutes(40, 60), ProjectB, TaskId: null, Note: null),
        ];
        var totals = TagTotals.Of(entries, Minutes(0, 60));

        // Act
        var clientTotal = totals.For(client);
        var websiteTotal = totals.For(website);
        var checkoutTotal = totals.For(checkout);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(60), clientTotal);
        Assert.Equal(TimeSpan.FromMinutes(60), websiteTotal);
        Assert.Equal(TimeSpan.FromMinutes(20), checkoutTotal);
    }

    [Fact]
    public void EmptyHasNoTime()
    {
        // Act
        var total = TagTotals.Empty.ForTask(ProjectA, taskId: null);

        // Assert
        Assert.Equal(TimeSpan.Zero, total);
    }

    private static TimeRange Minutes(int from, int to) => new(Nine.AddMinutes(from), Nine.AddMinutes(to));
}
