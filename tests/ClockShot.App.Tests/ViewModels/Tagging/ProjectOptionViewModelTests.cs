using ClockShot.App.ViewModels.Tagging;
using ClockShot.Core.Models;
using ClockShot.Core.Projects;

namespace ClockShot.App.Tests.ViewModels.Tagging;

public sealed class ProjectOptionViewModelTests
{
    private static readonly Project Website = new(Id: 10, ClientId: 1, ParentId: null, "Website", IsArchived: false);

    private static readonly ProjectCatalog Catalog = new(
        [new Client(Id: 1, "Client A", IsArchived: false)],
        [Website],
        []);

    private static readonly ProjectOptionViewModel Option = ProjectOptionViewModel.From(Website, Catalog);

    [Fact]
    public void TheFilterMatchesEveryTypedWordAnywhereInThePath()
    {
        // Act
        var matches = ProjectOptionViewModel.Filter("client site", Option);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void TheFilterRejectsAProjectMissingOneOfTheWords()
    {
        // Act
        var matches = ProjectOptionViewModel.Filter("client blog", Option);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void AnOptionShowsTheClientAsItsContext()
    {
        // Assert
        Assert.Equal("Website", Option.Name);
        Assert.Equal("Client A", Option.Context);
        Assert.True(Option.HasContext);
    }
}
