using Avalonia.Controls;
using Avalonia.Threading;

using ClockShot.App.Input;
using ClockShot.App.Services;
using ClockShot.App.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace ClockShot.App.Views;

public sealed partial class MainWindow : Window
{
    /// <summary>
    /// The main window with its view model, set as the owner of dialogs.
    /// </summary>
    public static MainWindow Create(IServiceProvider services)
    {
        var window = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };
        services.GetRequiredService<MainWindowHost>().Window = window;
        return window;
    }

    public MainWindow()
    {
        InitializeComponent();
        MenuShortcuts.Bind(this, MainMenu);
    }

    /// <summary>
    /// Decides what a close does.
    /// When the user closes the window, it hides to the tray if Close to tray is on, and otherwise the app exits.
    /// Exiting means the app doesn't keep running with no window and no way back to it.
    /// </summary>
    public static CloseAction ActionForClose(WindowCloseReason reason, bool isProgrammatic, bool closeToTray)
    {
        var isUserClose = reason == WindowCloseReason.WindowClosing && !isProgrammatic;
        if (!isUserClose)
        {
            return CloseAction.Close;
        }

        return closeToTray ? CloseAction.HideToTray : CloseAction.ExitApp;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        switch (ActionForClose(e.CloseReason, e.IsProgrammatic, viewModel.CloseToTray))
        {
            case CloseAction.HideToTray:
                e.Cancel = true;
                Hide();
                break;
            case CloseAction.ExitApp:
                e.Cancel = true;
                // Exit closes this window again, so it waits until this close has finished.
                Dispatcher.UIThread.Post(() => viewModel.ExitCommand.Execute(null));
                break;
        }
    }
}
