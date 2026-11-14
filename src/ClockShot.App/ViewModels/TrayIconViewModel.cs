using ClockShot.App.Services;

using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels;

/// <summary>
/// The tray icon and its menu.
/// </summary>
public sealed partial class TrayIconViewModel(TrackingStatusViewModel tracking, IAppLifetime lifetime) : ViewModelBase
{
    public TrackingStatusViewModel Tracking { get; } = tracking;

    /// <summary>
    /// Raised when the user asks to see the main window.
    /// The view layer shows it, so view models don't need to know about windows.
    /// </summary>
    public event Action? ShowWindowRequested;

    [RelayCommand]
    private void ShowWindow() => ShowWindowRequested?.Invoke();

    [RelayCommand]
    private void Exit() => lifetime.Shutdown();
}
