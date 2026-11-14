using ClockShot.App.Services;
using ClockShot.Core.Settings;

using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAppLifetime _lifetime;
    private readonly IShell _shell;
    private readonly IDialogService _dialogs;
    private readonly ISettingsStore _settings;
    private readonly AppPaths _paths;

    public MainWindowViewModel(
        SettingsViewModel settingsPage,
        TrackingStatusViewModel tracking,
        IAppLifetime lifetime,
        IShell shell,
        IDialogService dialogs,
        ISettingsStore settings,
        AppPaths paths)
    {
        Tracking = tracking;
        Pages = [settingsPage];
        _lifetime = lifetime;
        _shell = shell;
        _dialogs = dialogs;
        _settings = settings;
        _paths = paths;
    }

    public TrackingStatusViewModel Tracking { get; }

    /// <summary>
    /// The tabs, in display order.
    /// </summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    /// <summary>
    /// Whether closing the window hides it to the tray instead of exiting, so tracking keeps running.
    /// </summary>
    public bool CloseToTray => _settings.Current.General.CloseToTray;

    [RelayCommand]
    private void OpenLogFolder() => _shell.OpenFolder(_paths.Logs);

    [RelayCommand]
    private Task ShowAbout() => _dialogs.ShowAsync(new AboutViewModel());

    [RelayCommand]
    private void Exit() => _lifetime.Shutdown();
}
