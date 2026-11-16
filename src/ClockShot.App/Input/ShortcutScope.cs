namespace ClockShot.App.Input;

/// <summary>
/// Where a shortcut works and what binds it.
/// </summary>
public enum ShortcutScope
{
    /// <summary>
    /// The key works anywhere in the main window.
    /// <see cref="MenuShortcuts"/> binds it from the menu item that shows it.
    /// </summary>
    Window,

    /// <summary>
    /// The operating system handles the key, such as Alt+F4.
    /// It's listed but never bound.
    /// </summary>
    System,
}
