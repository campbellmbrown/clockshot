using ClockShot.App.ViewModels;

namespace ClockShot.App.Tests.ViewModels;

public sealed class DialogViewModelTests
{
    [Fact]
    public void CloseAsksForTheWindowToClose()
    {
        // Arrange
        var dialog = new TestDialog();
        var isCloseRequested = false;
        dialog.CloseRequested += () => isCloseRequested = true;

        // Act
        dialog.CloseCommand.Execute(null);

        // Assert
        Assert.True(isCloseRequested);
    }

    private sealed class TestDialog() : DialogViewModel("Test");
}
