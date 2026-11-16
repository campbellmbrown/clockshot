using ClockShot.App.Input;

namespace ClockShot.App.ViewModels;

/// <summary>
/// Help > Keyboard shortcuts, which lists <see cref="Shortcuts.All"/>.
/// Menus and key bindings use the same gestures, so changing a key changes it here too.
/// </summary>
public sealed class KeyboardShortcutsViewModel() : DialogViewModel("Keyboard shortcuts", canResize: true)
{
    public IReadOnlyList<ShortcutGroupViewModel> Groups { get; } =
        [.. Shortcuts.All.GroupBy(s => s.Group).Select(g => new ShortcutGroupViewModel(g.Key, [.. g]))];
}
