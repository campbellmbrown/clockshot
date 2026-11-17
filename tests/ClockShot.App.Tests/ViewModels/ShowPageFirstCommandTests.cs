using ClockShot.App.ViewModels;

using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.Tests.ViewModels;

public sealed class ShowPageFirstCommandTests
{
    [Fact]
    public void ShowsThePageThenRunsTheCommand()
    {
        // Arrange
        var steps = new List<string>();
        var command = new ShowPageFirstCommand(() => steps.Add("show page"));
        var pageCommand = new RelayCommand(() => steps.Add("run command"));

        // Act
        command.Execute(pageCommand);

        // Assert
        Assert.Equal(["show page", "run command"], steps);
    }

    [Fact]
    public void DoesNothingWhenTheCommandIsDisabled()
    {
        // Arrange
        var steps = new List<string>();
        var command = new ShowPageFirstCommand(() => steps.Add("show page"));
        var pageCommand = new RelayCommand(() => steps.Add("run command"), canExecute: () => false);

        // Act
        command.Execute(pageCommand);

        // Assert
        Assert.Empty(steps);
    }

    [Fact]
    public void PassesOnChangesToWhetherTheCommandCanRun()
    {
        // Arrange
        var command = new ShowPageFirstCommand(() => { });
        var pageCommand = new RelayCommand(() => { });
        var changes = 0;
        command.CanExecuteChanged += (_, _) => changes++;

        // A menu asks whether each item can run each time it opens, which is when the command starts following it.
        command.CanExecute(pageCommand);
        command.CanExecute(pageCommand);

        // Act
        pageCommand.NotifyCanExecuteChanged();

        // Assert
        Assert.Equal(1, changes);
    }
}
