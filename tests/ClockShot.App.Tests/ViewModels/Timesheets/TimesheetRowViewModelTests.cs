using ClockShot.App.ViewModels.Timesheets;
using ClockShot.Core.Projects;
using ClockShot.Core.Timesheets;

namespace ClockShot.App.Tests.ViewModels.Timesheets;

public sealed class TimesheetRowViewModelTests
{
    private static readonly TimesheetRow Task = new(
        CatalogRowKind.Task,
        "Login page",
        Cells: [TimeSpan.Zero, TimeSpan.FromMinutes(75)],
        Total: TimeSpan.FromMinutes(75),
        Notes: "",
        Children: []);

    private static readonly TimesheetRow Project = Task with { Kind = CatalogRowKind.Project, Name = "Website", Children = [Task] };

    [Fact]
    public void ARowInsideACollapsedRowIsHidden()
    {
        // Arrange
        var project = new TimesheetRowViewModel(Project, parent: null, depth: 0);
        var task = new TimesheetRowViewModel(Task, project, depth: 1);

        // Act
        project.IsExpanded = false;

        // Assert
        Assert.False(task.IsShown);
        Assert.True(project.IsShown);
    }

    [Fact]
    public void ACellWithNoTimeIsBlank()
    {
        // Act
        var row = new TimesheetRowViewModel(Task, parent: null, depth: 0);

        // Assert
        Assert.Equal(["", "1 h 15 min"], row.Cells);
    }
}
