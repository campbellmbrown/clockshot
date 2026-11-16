using Avalonia.Input;

namespace ClockShot.App.Input;

/// <summary>
/// One line of Help > Keyboard shortcuts.
/// </summary>
public sealed record Shortcut(string Group, string Keys, string Action, ShortcutScope Scope, KeyGesture? Gesture);
