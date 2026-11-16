using Avalonia.Input;

using ClockShot.Core;

namespace ClockShot.App.Input;

/// <summary>
/// Every keyboard shortcut, defined once.
/// Menus show these gestures and key bindings use them.
/// Changing a key here changes it everywhere.
/// Avoid function keys, Home, End, Page Up and Page Down, because they need the Fn key on many laptops.
/// Alt+F4 is listed only because Windows handles it.
/// </summary>
public static class Shortcuts
{
    // Avalonia's format that writes a gesture the way the platform's menus show it.
    private const string PlatformFormat = "p";

    // ---- Tabs ----
    public static KeyGesture ShowTimeline { get; } = KeyGesture.Parse("Ctrl+D1");
    public static KeyGesture ShowSettings { get; } = KeyGesture.Parse("Ctrl+D4");

    // ---- Other ----
    public static KeyGesture CloseWindow { get; } = KeyGesture.Parse("Alt+F4");

    /// <summary>
    /// Every shortcut, in the order Help > Keyboard shortcuts lists them.
    /// </summary>
    public static IReadOnlyList<Shortcut> All =>
    [
        Key("Tabs", ShowTimeline, "Timeline"),
        Key("Tabs", ShowSettings, "Settings"),

        Key(
            "Other",
            CloseWindow,
            $"Close the window ({AppInfo.Name} keeps running in the tray if that setting is on)",
            ShortcutScope.System),
    ];

    /// <summary>
    /// How a gesture is written for people, the same way menus show it.
    /// </summary>
    public static string Describe(KeyGesture gesture) => gesture.ToString(PlatformFormat, formatProvider: null);

    private static Shortcut Key(
        string group,
        KeyGesture gesture,
        string action,
        ShortcutScope scope = ShortcutScope.Window) =>
        new(group, Describe(gesture), action, scope, gesture);
}
