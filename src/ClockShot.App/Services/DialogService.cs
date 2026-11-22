using Avalonia.Controls;

using ClockShot.App.ViewModels;
using ClockShot.App.Views;

namespace ClockShot.App.Services;

/// <summary>
/// Shows each <see cref="DialogViewModel"/> in a <see cref="DialogWindow"/>, modal to the window it was opened from.
/// </summary>
public sealed class DialogService(MainWindowHost host) : IDialogService
{
    // The open dialogs shown without blocking, newest last.
    private readonly List<Window> _windowsShownWithoutBlocking = [];

    public async Task ShowAsync(DialogViewModel dialog)
    {
        var window = CreateWindow(dialog);
        if (OwnerForNewDialog() is { } owner)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            // The main window is hidden in the tray, so there's no window to block.
            await ShowModelessAsync(window);
        }
    }

    public void ShowWithoutBlocking(DialogViewModel dialog)
    {
        var window = CreateWindow(dialog);
        window.Topmost = true;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        _windowsShownWithoutBlocking.Add(window);
        window.Closed += (_, _) => _windowsShownWithoutBlocking.Remove(window);

        // Kept above the main window when it's showing. Otherwise it stands alone, above everything.
        if (host.VisibleWindow is { } owner)
        {
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
    }

    /// <summary>
    /// The window a new dialog opens over.
    /// A dialog shown without blocking stays on top, so a dialog opened from it must belong to it.
    /// The user is working in the active window, so that's the one a dialog was opened from.
    /// Otherwise it's the main window, or null when that's hidden in the tray.
    /// </summary>
    private Window? OwnerForNewDialog() =>
        _windowsShownWithoutBlocking.LastOrDefault(window => window.IsActive) ?? host.VisibleWindow;

    private static DialogWindow CreateWindow(DialogViewModel dialog)
    {
        var window = new DialogWindow { DataContext = dialog };
        dialog.CloseRequested += window.Close;
        window.Closed += (_, _) =>
        {
            dialog.CloseRequested -= window.Close;
            dialog.NotifyClosed();
        };
        return window;
    }

    private static Task ShowModelessAsync(Window window)
    {
        var closed = new TaskCompletionSource();
        window.Closed += (_, _) => closed.TrySetResult();
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.Show();
        return closed.Task;
    }
}
