using ClockShot.App.ViewModels.Dialogs;

namespace ClockShot.App.Tests.ViewModels.Dialogs;

public sealed class TextPromptViewModelTests
{
    private static TextPromptViewModel Prompt(string initialText = "") =>
        new(title: "New client", label: "Client name", initialText: initialText, confirmText: "Add client");

    [Fact]
    public void ConfirmingGivesTheTrimmedTextAndCloses()
    {
        // Arrange
        var prompt = Prompt();
        var isCloseRequested = false;
        prompt.CloseRequested += () => isCloseRequested = true;
        prompt.Text = "  Client A  ";

        // Act
        prompt.ConfirmCommand.Execute(null);

        // Assert
        Assert.Equal("Client A", prompt.Result);
        Assert.True(isCloseRequested);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankTextCantBeConfirmed(string text)
    {
        // Arrange
        var prompt = Prompt();

        // Act
        prompt.Text = text;

        // Assert
        Assert.False(prompt.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void CancellingLeavesNoResult()
    {
        // Arrange
        var prompt = Prompt(initialText: "Client A");

        // Act
        prompt.CloseCommand.Execute(null);

        // Assert
        Assert.Null(prompt.Result);
    }

    [Fact]
    public void StartsWithTheInitialTextAndConfirmText()
    {
        // Act
        var prompt = Prompt(initialText: "Client A");

        // Assert
        Assert.Equal("Client A", prompt.Text);
        Assert.Equal("Add client", prompt.ConfirmText);
    }
}
