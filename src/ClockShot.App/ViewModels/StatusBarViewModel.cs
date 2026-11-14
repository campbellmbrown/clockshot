using ClockShot.Core.Models;
using ClockShot.Core.Tracking;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels;

/// <summary>
/// The bar at the bottom of the window.
/// It shows the tracking state and the window being recorded.
/// </summary>
public sealed partial class StatusBarViewModel : ViewModelBase
{
    public StatusBarViewModel(TrackingStatusViewModel trackingStatus)
    {
        Tracking = trackingStatus;

        // TrackingStatusViewModel already raises this on the UI thread.
        Tracking.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TrackingStatusViewModel.Status))
            {
                ShowStatus();
            }
        };
        ShowStatus();
    }

    /// <summary>
    /// Pauses and resumes tracking when the state is clicked.
    /// </summary>
    public TrackingStatusViewModel Tracking { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateText))]
    public partial StatusBarState State { get; private set; }

    public string StateText => State switch
    {
        StatusBarState.Tracking => "Tracking",
        StatusBarState.Paused => "Paused",
        StatusBarState.Away => "Away",
        StatusBarState.Locked => "Locked",
        StatusBarState.Starting => "Starting",
        _ => throw new InvalidOperationException($"Unknown status bar state {State}."),
    };

    /// <summary>
    /// The application and title of the window being recorded.
    /// Empty when nothing is being recorded.
    /// </summary>
    [ObservableProperty]
    public partial string WindowText { get; private set; } = "";

    public static StatusBarState StateOf(TrackingStatus status) => status switch
    {
        { IsPaused: true } => StatusBarState.Paused,
        { State: ComputerState.Active } => StatusBarState.Tracking,
        { State: ComputerState.Away } => StatusBarState.Away,
        { State: ComputerState.Locked } => StatusBarState.Locked,
        _ => StatusBarState.Starting,
    };

    public static string WindowTextOf(TrackingStatus status) =>
        status.Window is { } window ? $"{window.DisplayName} - {window.Title}" : "";

    private void ShowStatus()
    {
        var status = Tracking.Status;
        State = StateOf(status);
        WindowText = WindowTextOf(status);
    }
}
