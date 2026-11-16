using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ClockShot.App.Input;

/// <summary>
/// Makes the window-wide shortcuts shown in menus work while the menus are closed.
/// Avalonia handles a menu item's key only while its menu is open.
/// Each of these shortcuts becomes a key binding on the window that runs the menu item's command.
/// Each key is then declared once, on its menu item.
/// </summary>
public static class MenuShortcuts
{
    public static void Bind(Window window, Menu menu)
    {
        var windowKeys = Shortcuts.All.Where(s => s.Scope == ShortcutScope.Window).Select(s => s.Gesture).ToHashSet();
        foreach (var item in AllMenuItems(menu))
        {
            if (item.InputGesture is not { } gesture || !windowKeys.Contains(gesture))
            {
                continue;
            }

            var binding = new KeyBinding { Gesture = gesture };

            // MenuItem.Command can be null, but KeyBinding.Command isn't declared nullable.
            binding.Bind<ICommand>(KeyBinding.CommandProperty, item.GetObservable(MenuItem.CommandProperty)!);
            binding.Bind(KeyBinding.CommandParameterProperty, item.GetObservable(MenuItem.CommandParameterProperty));
            window.KeyBindings.Add(binding);
        }
    }

    private static IEnumerable<MenuItem> AllMenuItems(ItemsControl parent) =>
        parent.Items.OfType<MenuItem>().SelectMany(item => AllMenuItems(item).Prepend(item));
}
