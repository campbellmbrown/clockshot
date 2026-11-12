using ClockShot.App.ViewModels;
using ClockShot.App.Views;

namespace ClockShot.App.Services;

/// <summary>
/// Shows each <see cref="DialogViewModel"/> in a <see cref="DialogWindow"/>, modal to the main window.
/// </summary>
public sealed class DialogService(MainWindowHost host) : IDialogService
{
    public async Task ShowAsync(DialogViewModel dialog)
    {
        var window = CreateWindow(dialog);
        await window.ShowDialog(host.Window);
    }

    private static DialogWindow CreateWindow(DialogViewModel dialog)
    {
        var window = new DialogWindow { DataContext = dialog };
        dialog.CloseRequested += window.Close;
        window.Closed += (_, _) =>
        {
            dialog.CloseRequested -= window.Close;
        };
        return window;
    }
}
