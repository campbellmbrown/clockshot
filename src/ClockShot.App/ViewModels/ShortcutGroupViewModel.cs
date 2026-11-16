using ClockShot.App.Input;

namespace ClockShot.App.ViewModels;

/// <summary>
/// One heading of Help > Keyboard shortcuts, with the shortcuts under it.
/// </summary>
public sealed record ShortcutGroupViewModel(string Title, IReadOnlyList<Shortcut> Shortcuts);
