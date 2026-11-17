using System.Windows.Input;

namespace ClockShot.App.ViewModels;

/// <summary>
/// Runs a page's command from the menu or a shortcut.
/// It shows the page first, so the change is never made out of sight.
/// The page's command is passed as the command parameter, and this command is enabled exactly when that one is.
/// </summary>
public sealed class ShowPageFirstCommand(Action showPage) : ICommand
{
    private readonly HashSet<ICommand> _watched = [];

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (parameter is not ICommand command)
        {
            return false;
        }

        // A menu asks whether each item can run before showing it, so this is where its command is first seen.
        if (_watched.Add(command))
        {
            command.CanExecuteChanged += (_, _) => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        return command.CanExecute(null);
    }

    public void Execute(object? parameter)
    {
        if (parameter is not ICommand command || !command.CanExecute(null))
        {
            return;
        }

        showPage();
        command.Execute(null);
    }
}
