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
    /// The key works only while the timeline has focus.
    /// TimelineView binds it.
    /// </summary>
    Timeline,

    /// <summary>
    /// The operating system handles the key, such as Alt+F4.
    /// It's listed but never bound.
    /// </summary>
    System,

    /// <summary>
    /// The shortcut is a mouse action, which the list only describes.
    /// </summary>
    Mouse,
}
