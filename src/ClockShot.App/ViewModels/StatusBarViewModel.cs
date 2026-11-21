using ClockShot.App.Services;
using ClockShot.Core.Models;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Core.Tracking;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels;

/// <summary>
/// The bar at the bottom of the window.
/// It shows the tracking state, the window being recorded, and today's active and tagged time.
/// </summary>
public sealed partial class StatusBarViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan TotalsRefreshInterval = TimeSpan.FromMinutes(1);

    private readonly IActivityHistory _history;
    private readonly ITimeEntryRepository _entries;
    private readonly DayClock _days;
    private readonly BackgroundWork _background;
    private readonly ITimer _timer;

    public StatusBarViewModel(
        TrackingStatusViewModel trackingStatus,
        IActivityHistory history,
        ITimeEntryRepository entries,
        Tagger tagger,
        DayClock days,
        TimeProvider time,
        BackgroundWork background)
    {
        Tracking = trackingStatus;
        _history = history;
        _entries = entries;
        _days = days;
        _background = background;

        // TrackingStatusViewModel already raises this on the UI thread.
        Tracking.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TrackingStatusViewModel.Status))
            {
                ShowStatus();
            }
        };
        tagger.Changed += () => OnUiThread(RefreshTotalsInBackground);
        _timer = time.CreateTimer(
            _ => OnUiThread(RefreshTotalsInBackground),
            state: null,
            dueTime: TotalsRefreshInterval,
            period: TotalsRefreshInterval);
        ShowStatus();
        RefreshTotalsInBackground();
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

    /// <summary>
    /// Today's active and tagged time.
    /// </summary>
    [ObservableProperty]
    public partial string TodayText { get; private set; } = "";

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

    public async Task RefreshTotalsAsync()
    {
        var today = _days.RangeOf(_days.Today);

        var records = await _history.LoadAsync(today);
        var tags = await _entries.GetAsync(today);
        var active = ActivitySummary.TimeIn(records.States, ComputerState.Active, today);
        var tagged = TagTotals.Of(tags, today).Total;
        TodayText = $"Today: active {DurationText.Format(active)} - tagged {DurationText.Format(tagged)}";
    }

    public void Dispose() => _timer.Dispose();

    private void ShowStatus()
    {
        var status = Tracking.Status;
        State = StateOf(status);
        WindowText = WindowTextOf(status);
    }

    private void RefreshTotalsInBackground() => _background.Run(RefreshTotalsAsync);
}
