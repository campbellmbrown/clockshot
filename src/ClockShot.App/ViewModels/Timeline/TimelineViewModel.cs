using ClockShot.App.Controls;
using ClockShot.App.Services;
using ClockShot.Core.Models;
using ClockShot.Core.Settings;
using ClockShot.Core.Timeline;
using ClockShot.Core.Tracking;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// The Timeline tab, which shows one day of recorded activity.
/// </summary>
public sealed partial class TimelineViewModel : PageViewModel, IDisposable
{
    private const int TicksPerRefresh = 3;

    /// <summary>
    /// The now line moves on every tick.
    /// Today's records are reloaded every <see cref="TicksPerRefresh"/> ticks.
    /// </summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How far behind now a block's saved end can be while the block is still open.
    /// It's the tracker's save interval plus the time between reloads, with 15 seconds to spare.
    /// Blocks that end closer to now than this are drawn up to now.
    /// </summary>
    private static readonly TimeSpan StillOpenWithin =
        TrackingTimings.SaveInterval + TickInterval * TicksPerRefresh + TimeSpan.FromSeconds(15);

    private readonly IActivityHistory _history;
    private readonly ITrackingController _tracking;
    private readonly DayClock _days;
    private readonly ILogger<TimelineViewModel> _logger;
    private readonly BackgroundWork _background;
    private readonly ITimer _timer;

    private int _ticks;

    // The last day start hour seen, so a settings change that keeps it doesn't reload the day.
    private int _dayStartHour;

    private ActivityRecords _records = ActivityRecords.Empty;
    private CancellationTokenSource? _loading;

    public TimelineViewModel(
        IActivityHistory history,
        ITrackingController tracking,
        ISettingsStore settings,
        TimeProvider time,
        DayClock days,
        ILogger<TimelineViewModel> logger,
        BackgroundWork background)
        : base("Timeline", Icons.Timeline)
    {
        _history = history;
        _tracking = tracking;
        _days = days;
        _logger = logger;
        _background = background;

        Zoom = new TimelineZoom();

        Zoom.ViewportChanged += () => RedrawNeeded?.Invoke();
        _dayStartHour = settings.Current.General.DayStartHour;
        settings.Changed += changed => OnUiThread(() => OnSettingsChanged(changed));
        NowUtc = Now();

        // Setting the day loads it, through OnDayChanged.
        Day = Today();

        _timer = time.CreateTimer(
            _ => OnUiThread(OnTick),
            state: null,
            dueTime: TickInterval,
            period: TickInterval);
    }

    /// <summary>
    /// Raised when anything the timeline control draws has changed.
    /// </summary>
    public event Action? RedrawNeeded;

    public TimelineZoom Zoom { get; }

    public TimeZoneInfo TimeZone => _days.Zone;

    /// <summary>
    /// The day being shown.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDate), nameof(DayRange))]
    [NotifyCanExecuteChangedFor(nameof(NextDayCommand))]
    public partial DateOnly Day { get; set; }

    /// <summary>
    /// <see cref="Day"/> for the calendar date picker, which works with <see cref="DateTime"/>.
    /// </summary>
    public DateTime? SelectedDate
    {
        get => Day.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (value is { } date)
            {
                var picked = DateOnly.FromDateTime(date);
                Day = picked > Today() ? Today() : picked;
            }
        }
    }

    /// <summary>
    /// The calendar can't go past today.
    /// </summary>
    public DateTime LatestDate => Today().ToDateTime(TimeOnly.MinValue);

    public TimeRange DayRange => _days.RangeOf(Day);

    [ObservableProperty]
    public partial IReadOnlyList<TimelineLane> Lanes { get; private set; } = [];

    /// <summary>
    /// The start, end and length of the working day with the time spent active, or why there's nothing to show.
    /// </summary>
    [ObservableProperty]
    public partial string SummaryText { get; private set; } = "";

    /// <summary>
    /// Applications used on this day, most used first.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDayApplications))]
    public partial IReadOnlyList<ApplicationTotalViewModel> DayApplications { get; private set; } = [];

    public bool HasDayApplications => DayApplications.Count > 0;

    /// <summary>
    /// The current time, drawn as the now line when it's inside the day.
    /// </summary>
    [ObservableProperty]
    public partial DateTime NowUtc { get; private set; }

    /// <summary>
    /// From the first to the last active time of the day, or null if the computer was never active.
    /// </summary>
    [ObservableProperty]
    public partial TimeRange? WorkingDay { get; private set; }

    private bool IsToday => Day == Today();

    /// <summary>
    /// Loads the day's records.
    /// A newer load cancels an unfinished older one.
    /// </summary>
    public async Task LoadAsync()
    {
        var loading = Restart();
        try
        {
            var records = await _history.LoadAsync(DayRange, loading.Token);
            if (loading.IsCancellationRequested)
            {
                return;
            }

            _records = records;
            ShowRecords();
        }
        catch (OperationCanceledException)
        {
            // A newer load took over.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loading the timeline for {Day} failed", Day);
            SummaryText = "This day couldn't be loaded. Details are in the log (Help > Open log folder).";
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _loading?.Cancel();
        _loading?.Dispose();
    }

    partial void OnDayChanged(DateOnly value) => ShowDay();

    // The toolkit calls these when a property the control draws has changed.
    partial void OnLanesChanged(IReadOnlyList<TimelineLane> value) => RedrawNeeded?.Invoke();

    partial void OnNowUtcChanged(DateTime value) => RedrawNeeded?.Invoke();

    /// <summary>
    /// Refits and reloads the day when the day start hour changes.
    /// </summary>
    private void OnSettingsChanged(AppSettings settings)
    {
        if (settings.General.DayStartHour == _dayStartHour)
        {
            return;
        }

        _dayStartHour = settings.General.DayStartHour;
        OnPropertyChanged(nameof(LatestDate));
        NextDayCommand.NotifyCanExecuteChanged();

        // A later hour can make the shown day start in the future, so the timeline moves to today.
        // An earlier hour keeps the shown day, as the normal change of day at the start hour does.
        if (Day > Today())
        {
            Day = Today();
            return;
        }

        OnPropertyChanged(nameof(DayRange));
        ShowDay();
    }

    /// <summary>
    /// Fits the whole day in view and loads it.
    /// </summary>
    private void ShowDay()
    {
        Zoom.Fit(DayRange);
        _background.Run(() => LoadAsync());
    }

    [RelayCommand]
    private void PreviousDay() => Day = Day.AddDays(-1);

    [RelayCommand(CanExecute = nameof(CanGoToNextDay))]
    private void NextDay() => Day = Day.AddDays(1);

    private bool CanGoToNextDay() => Day < Today();

    /// <summary>
    /// Shows today, scrolled so the current time is visible.
    /// </summary>
    [RelayCommand]
    private void GoToToday()
    {
        Day = Today();
        Zoom.CenterOn(NowUtc);
    }

    private string Summarize(ActivityRecords records)
    {
        if (WorkingDay is not { } working)
        {
            return "No active time on this day.";
        }

        var active = ActivitySummary.TimeIn(records.States, ComputerState.Active, DayRange);
        return string.Join("  |  ",
            $"Day start {LocalTime.Format(working.Start, TimeZone)}",
            $"Day end {LocalTime.Format(working.End, TimeZone)}",
            $"Duration {DurationText.Format(working.Duration)}",
            $"Active {DurationText.Format(active)}");
    }

    private void OnTick()
    {
        NowUtc = Now();
        OnPropertyChanged(nameof(LatestDate));
        NextDayCommand.NotifyCanExecuteChanged();
        if (!IsToday)
        {
            return;
        }

        _ticks++;
        if (_ticks % TicksPerRefresh == 0)
        {
            _background.Run(() => LoadAsync());
        }
        else
        {
            // Keeps the open blocks reaching the now line between reloads.
            // The totals wait for the next reload.
            ShowLanes(WithOpenBlocksUpToNow(_records));
        }
    }

    /// <summary>
    /// Builds the lanes, applications list and summary from the loaded records, drawing still-open blocks up to now.
    /// </summary>
    private void ShowRecords()
    {
        var records = WithOpenBlocksUpToNow(_records);
        ShowLanes(records);
        var totals = ActivitySummary.ByApplication(records.Activities, DayRange);
        DayApplications = ApplicationTotalViewModel.From(totals, records.Applications);

        // Summarize reads WorkingDay, so it's set first.
        WorkingDay = ActivitySummary.WorkingDay(records.States, DayRange);
        SummaryText = Summarize(records);
    }

    private void ShowLanes(ActivityRecords records) => Lanes = TimelineLanes.Build(records);

    private ActivityRecords WithOpenBlocksUpToNow(ActivityRecords records) =>
        IsToday && !_tracking.Status.IsPaused ? OpenBlocks.UpTo(records, NowUtc, StillOpenWithin) : records;

    private DateTime Now() => _days.NowUtc;

    private DateOnly Today() => _days.Today;

    /// <summary>
    /// Cancels the previous load and starts a new one.
    /// The old source isn't disposed, because its load may still be checking it.
    /// </summary>
    private CancellationTokenSource Restart()
    {
        _loading?.Cancel();
        return _loading = new CancellationTokenSource();
    }
}
