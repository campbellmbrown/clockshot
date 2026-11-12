using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;

using ClockShot.App.Controls;
using ClockShot.App.ViewModels;
using ClockShot.Core;

namespace ClockShot.App.Views;

/// <summary>
/// The tray icon and its menu.
/// </summary>
public static class TrayIconView
{
    public static void Show(Application app, TrayIconViewModel tray)
    {
        var icon = new TrayIcon
        {
            Icon = AppIcon.Normal,
            ToolTipText = AppInfo.Name,
            Command = tray.ShowWindowCommand,
            Menu =
            [
                Item($"Open {AppInfo.Name}", tray.ShowWindowCommand),
                new NativeMenuItemSeparator(),
                Item("Exit", tray.ExitCommand),
            ],
        };

        TrayIcon.SetIcons(app, [icon]);
    }

    private static NativeMenuItem Item(string header, ICommand command) => new() { Header = header, Command = command };
}
