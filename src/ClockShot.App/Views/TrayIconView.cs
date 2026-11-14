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
        var tracking = tray.Tracking;
        var pauseItem = new NativeMenuItem { Command = tracking.TogglePausedCommand };
        var icon = new TrayIcon
        {
            Command = tray.ShowWindowCommand,
            Menu =
            [
                Item($"Open {AppInfo.Name}", tray.ShowWindowCommand),
                pauseItem,
                new NativeMenuItemSeparator(),
                Item("Exit", tray.ExitCommand),
            ],
        };

        void ShowTrackingState()
        {
            icon.Icon = AppIcon.For(tracking.IsPaused);
            icon.ToolTipText = tracking.StatusText;
            pauseItem.Header = tracking.PauseCommandText;
        }

        ShowTrackingState();
        tracking.PropertyChanged += (_, _) => ShowTrackingState();
        TrayIcon.SetIcons(app, [icon]);
    }

    private static NativeMenuItem Item(string header, ICommand command) => new() { Header = header, Command = command };
}
