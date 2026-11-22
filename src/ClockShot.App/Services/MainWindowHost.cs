using Avalonia.Controls;

namespace ClockShot.App.Services;

/// <summary>
/// Holds the main window once it exists, for services that need one.
/// Dialogs open over it, and the clipboard and file pickers belong to it.
/// </summary>
public sealed class MainWindowHost
{
    private Window? _window;

    /// <summary>
    /// The main window. MainWindow.Create sets it once at startup, so reading it earlier is a bug.
    /// </summary>
    public Window Window
    {
        get => _window ?? throw new InvalidOperationException("The main window hasn't been created yet.");
        set => _window = value;
    }

    /// <summary>
    /// The main window if it's showing, or null when it's hidden in the tray.
    /// </summary>
    public Window? VisibleWindow => _window is { IsVisible: true } window ? window : null;
}
