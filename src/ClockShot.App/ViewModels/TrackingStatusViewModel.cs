using ClockShot.App.Controls;
using ClockShot.Core;
using ClockShot.Core.Tracking;

using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels;

/// <summary>
/// Whether tracking is running, and the command that pauses or resumes it.
/// The menu, the tray icon and the status bar share it.
/// </summary>
public sealed partial class TrackingStatusViewModel : ViewModelBase
{
    private readonly ITrackingController _tracking;

    public TrackingStatusViewModel(ITrackingController tracking)
    {
        _tracking = tracking;
        _tracking.StatusChanged += () => OnUiThread(() =>
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(PauseCommandText));
            OnPropertyChanged(nameof(PauseCommandIconKind));
            OnPropertyChanged(nameof(StatusText));
        });
    }

    public TrackingStatus Status => _tracking.Status;

    public bool IsPaused => Status.IsPaused;

    public string PauseCommandText => IsPaused ? "Resume tracking" : "Pause tracking";

    public string PauseCommandIconKind => IsPaused ? Icons.ResumeTracking : Icons.PauseTracking;

    public string StatusText => IsPaused ? $"{AppInfo.Name}: tracking paused" : $"{AppInfo.Name}: tracking";

    [RelayCommand]
    private void TogglePaused()
    {
        if (IsPaused)
        {
            _tracking.Resume();
        }
        else
        {
            _tracking.Pause();
        }
    }
}
