using ClockShot.App.ViewModels;
using ClockShot.App.ViewModels.Projects;
using ClockShot.Core.Projects;
using ClockShot.Data.Projects;

namespace ClockShot.App.Tests.ViewModels;

public sealed class DialogViewModelTests
{
    [Fact]
    public void CloseAsksForTheWindowToClose()
    {
        // Arrange
        var dialog = new AboutViewModel();
        var isCloseRequested = false;
        dialog.CloseRequested += () => isCloseRequested = true;

        // Act
        dialog.CloseCommand.Execute(null);

        // Assert
        Assert.True(isCloseRequested);
    }

    [Fact]
    public void ADialogWithNothingToConfirmClosesOnEnter()
    {
        // Act
        var dialog = new AboutViewModel();

        // Assert
        Assert.Equal("Close", dialog.CloseText);
        Assert.True(dialog.ClosesOnEnter);
    }

    [Fact]
    public void ADialogWithSomethingToConfirmOffersCancelAndDoesNotCloseOnEnter()
    {
        // Act
        var dialog = ProjectDialogViewModel.ForNew(new ProjectRepository(UnopenedDatabase.Create()), ProjectCatalog.Empty);

        // Assert
        Assert.Equal("Cancel", dialog.CloseText);
        Assert.False(dialog.ClosesOnEnter);
    }
}
