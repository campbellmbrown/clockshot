using Avalonia.Input;

using ClockShot.Core;

namespace ClockShot.App.Input;

/// <summary>
/// Every keyboard and mouse shortcut, defined once.
/// Menus show these gestures and key bindings use them.
/// The timeline reads its mouse modifiers from here.
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

    // ---- Timeline ----
    public static KeyGesture PreviousDay { get; } = KeyGesture.Parse("Alt+Left");
    public static KeyGesture NextDay { get; } = KeyGesture.Parse("Alt+Right");
    public static KeyGesture Today { get; } = KeyGesture.Parse("Ctrl+T");
    public static KeyGesture ZoomIn { get; } = KeyGesture.Parse("Ctrl+OemPlus");
    public static KeyGesture ZoomOut { get; } = KeyGesture.Parse("Ctrl+OemMinus");
    public static KeyGesture FitWorkingDay { get; } = KeyGesture.Parse("Ctrl+D9");
    public static KeyGesture ShowWholeDay { get; } = KeyGesture.Parse("Ctrl+D0");
    public static KeyGesture ClearSelection { get; } = KeyGesture.Parse("Escape");

    // TimelineControl reads these mouse modifiers.
    public static KeyModifiers ScrollModifier { get; } = KeyModifiers.Shift;
    public static KeyModifiers NoSnapModifier { get; } = KeyModifiers.Alt;

    // ---- Tagging ----
    public static KeyGesture TagSelection { get; } = KeyGesture.Parse("Enter");
    public static KeyGesture DeleteTag { get; } = KeyGesture.Parse("Delete");
    public static KeyGesture Undo { get; } = KeyGesture.Parse("Ctrl+Z");

    // ---- Screenshots ----
    public static KeyGesture SwitchPreview { get; } = KeyGesture.Parse("Ctrl+P");
    public static KeyGesture PausePreview { get; } = KeyGesture.Parse("Ctrl+L");

    // ---- Other ----
    public static KeyGesture CloseWindow { get; } = KeyGesture.Parse("Alt+F4");

    /// <summary>
    /// Every shortcut, in the order Help > Keyboard shortcuts lists them.
    /// </summary>
    public static IReadOnlyList<Shortcut> All =>
    [
        Key("Tabs", ShowTimeline, "Timeline"),
        Key("Tabs", ShowSettings, "Settings"),

        Key("Timeline", PreviousDay, "Previous day"),
        Key("Timeline", NextDay, "Next day"),
        Key("Timeline", Today, "Today, scrolled to now"),
        Key("Timeline", ZoomIn, "Zoom in"),
        Key("Timeline", ZoomOut, "Zoom out"),
        Key("Timeline", FitWorkingDay, "Fit to the working day"),
        Key("Timeline", ShowWholeDay, "Show the whole day"),
        Key("Timeline", ClearSelection, "Clear the selection", ShortcutScope.Timeline),
        MouseWithModifier("Timeline", ScrollModifier, "+wheel", "Scroll"),
        MouseWithModifier("Timeline", NoSnapModifier, " while dragging", "Don't snap to the edges of blocks"),
        MouseOnly("Timeline", "Mouse wheel", "Zoom around the pointer"),
        MouseOnly("Timeline", "Drag the ruler", "Scroll"),
        MouseOnly("Timeline", "Drag across the rows", "Select a time range"),
        MouseOnly("Timeline", "Drag an edge of the selection", "Make it longer or shorter (on a tag: changes the tag)"),

        Key("Tagging", TagSelection, "Tag the selected time (goes to the project box)", ShortcutScope.Timeline),
        Key("Tagging", DeleteTag, "Delete the selected tag", ShortcutScope.Timeline),
        Key("Tagging", Undo, "Undo the last change"),

        Key("Screenshots", SwitchPreview, "Switch where the hover preview appears"),
        Key("Screenshots", PausePreview, "Pause the preview where it is / resume"),

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

    private static Shortcut MouseWithModifier(string group, KeyModifiers modifier, string suffix, string action) =>
        new(group, $"{modifier}{suffix}", action, ShortcutScope.Mouse, Gesture: null);

    /// <summary>
    /// A mouse action with no key to change. TimelineControl handles it.
    /// </summary>
    private static Shortcut MouseOnly(string group, string keys, string action) =>
        new(group, keys, action, ShortcutScope.Mouse, Gesture: null);
}
