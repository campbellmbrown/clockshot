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
/// It zooms and scrolls, previews screenshots on hover and shows the details of the selection.
/// <see cref="Controls.TimelineControl"/> draws it and passes the pointer input here.
/// </summary>
public sealed partial class TimelineViewModel : PageViewModel, IDisposable
{
    private const int TicksPerRefresh = 3;

    /// <summary>
    /// How close the pointer must be to an edge to snap to it, in pixels.
    /// </summary>
    private const double SnapDistancePixels = 8;

    /// <summary>
    /// How close the pointer must be to an edge to grab it, in pixels.
    /// </summary>
    private const double EdgeGrabPixels = 5;

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
    private ScreenshotIndex _screenshots = ScreenshotIndex.Empty;
    private CancellationTokenSource? _loading;

    // The pointer's last position and snap flag, kept so the hover follows when the view moves or the day's rows reload.
    private double? _hoverX;
    private bool _hoverSnaps;

    public TimelineViewModel(
        IActivityHistory history,
        ITrackingController tracking,
        ISettingsStore settings,
        TimeProvider time,
        DayClock days,
        ScreenshotPreviewViewModel preview,
        SelectionDetailsViewModel details,
        ILogger<TimelineViewModel> logger,
        BackgroundWork background)
        : base("Timeline", Icons.Timeline)
    {
        _history = history;
        _tracking = tracking;
        _days = days;
        _logger = logger;
        _background = background;

        Preview = preview;
        Details = details;
        Details.SetDay(ActivityRecords.Empty, TimeZone);
        Zoom = new TimelineZoom();
        Selection = new TimelineSelection(details);

        Selection.Changed += () =>
        {
            RedrawNeeded?.Invoke();
            ZoomToSelectionCommand.NotifyCanExecuteChanged();
            ClearSelectionCommand.NotifyCanExecuteChanged();
        };
        preview.PauseChanged += () =>
        {
            // Resuming shows what's under the pointer straight away, instead of on its next move.
            ShowHover();
            RedrawNeeded?.Invoke();
        };
        Zoom.ViewportChanged += () =>
        {
            ShowHover();
            RedrawNeeded?.Invoke();
        };
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

    public ScreenshotPreviewViewModel Preview { get; }

    public SelectionDetailsViewModel Details { get; }

    public TimelineZoom Zoom { get; }

    public TimelineSelection Selection { get; }

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
    [NotifyCanExecuteChangedFor(nameof(FitWorkingDayCommand))]
    public partial TimeRange? WorkingDay { get; private set; }

    /// <summary>
    /// The time under the pointer, or null when it isn't over the timeline.
    /// </summary>
    [ObservableProperty]
    public partial DateTime? HoverTimeUtc { get; private set; }

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
            _screenshots = new ScreenshotIndex(records.Screenshots);
            Details.SetDay(_records, TimeZone);
            ShowRecords();
            Selection.Refresh(Lanes);
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

    /// <summary>
    /// The pointer moved over the timeline.
    /// </summary>
    /// <param name="x">The pointer's distance from the left edge of the drawing area.</param>
    /// <param name="snap">False while the no-snap key is held.</param>
    public void HoverAt(double x, bool snap)
    {
        _hoverX = x;
        _hoverSnaps = snap;
        ShowHover();
    }

    /// <summary>
    /// The pointer left the timeline.
    /// </summary>
    public void EndHover()
    {
        _hoverX = null;
        HoverTimeUtc = null;
        Selection.ShowSnapPreview(null);
        Preview.IsHovering = false;
    }

    /// <summary>
    /// A click on a lane selects the block under the pointer, or clears the selection when there's none.
    /// </summary>
    public void ClickAt(DateTime time, LaneKind? lane)
    {
        if (lane is { } kind && Lane(kind)?.BlockAt(time) is { } block)
        {
            Selection.SelectBlock(kind, block);
        }
        else
        {
            ClearSelection();
        }
    }

    /// <summary>
    /// True when pressing at the time would grab an edge, so the control shows the resize cursor.
    /// </summary>
    public bool IsOverEdge(DateTime time) => FindEdge(time) is not null;

    /// <summary>
    /// A press on an edge of the selection starts dragging that edge.
    /// Returns false when there's no edge under the pointer.
    /// </summary>
    public bool TryGrabEdge(DateTime time)
    {
        if (FindEdge(time) is not { } anchor)
        {
            return false;
        }

        Selection.GrabEdge(anchor);
        return true;
    }

    /// <summary>
    /// The pointer moved far enough while pressed, so a new selection starts where it was pressed.
    /// </summary>
    public void StartSelecting(DateTime pressedAt, bool snap) => Selection.StartDrag(SnapIf(snap, pressedAt));

    public void DragTo(DateTime time, bool snap) => Selection.DragTo(SnapIf(snap, time));

    public void EndDrag() => Selection.EndDrag();

    public void Dispose()
    {
        _timer.Dispose();
        _loading?.Cancel();
        _loading?.Dispose();
        // Preview comes from the service container, which disposes it.
    }

    partial void OnDayChanged(DateOnly value) => ShowDay();

    // The toolkit calls these when a property the control draws has changed.
    partial void OnLanesChanged(IReadOnlyList<TimelineLane> value)
    {
        ShowHover();
        RedrawNeeded?.Invoke();
    }

    partial void OnHoverTimeUtcChanged(DateTime? value) => RedrawNeeded?.Invoke();

    partial void OnNowUtcChanged(DateTime value) => RedrawNeeded?.Invoke();

    /// <summary>
    /// Shows the time, snap preview and screenshot or message under the pointer.
    /// </summary>
    private void ShowHover()
    {
        if (_hoverX is not { } x)
        {
            return;
        }

        var time = Zoom.Viewport.TimeAt(x);
        HoverTimeUtc = time;
        var edge = _hoverSnaps && !Selection.IsDragging ? SnapToEdge(time) : time;
        Selection.ShowSnapPreview(edge != time ? edge : null);
        Preview.IsHovering = true;
        if (Preview.IsPaused)
        {
            return;
        }

        Preview.ShownAtUtc = time;

        var preview = HoverPreview.At(Lanes, _screenshots, time);
        if (preview is { Window: { } window, Screenshot: { } screenshot })
        {
            var takenText = LocalTime.FormatWithSeconds(screenshot.TakenUtc, TimeZone);
            _background.Run(() => Preview.ShowAsync(
                screenshot,
                takenText,
                window.Application.DisplayName,
                window.Title));
        }
        else if (preview.Window is { } windowWithoutScreenshot)
        {
            Preview.ShowMessage(
                "No screenshot of this window.",
                windowWithoutScreenshot.Application.DisplayName,
                windowWithoutScreenshot.Title);
        }
        else
        {
            Preview.ShowMessage(preview.Kind switch
            {
                HoverPreviewKind.Away => "No screenshots while away.",
                HoverPreviewKind.Locked => "No screenshots while locked.",
                _ => "Nothing was recorded at this time.",
            });
        }
    }

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
        Preview.Resume();

        // The last time shown was on the old day, so there's nothing to pause on until the next hover.
        Preview.ShownAtUtc = null;
        Zoom.Fit(DayRange);
        ClearSelection();
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

    [RelayCommand]
    private void ZoomIn() => Zoom.ZoomAt(ZoomAnchorX(), steps: 1);

    [RelayCommand]
    private void ZoomOut() => Zoom.ZoomAt(ZoomAnchorX(), steps: -1);

    [RelayCommand]
    private void FitDay() => Zoom.Fit(DayRange);

    /// <summary>
    /// Zooms to the working day, with a margin either side.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasWorkingDay))]
    private void FitWorkingDay()
    {
        if (WorkingDay is { } working)
        {
            Zoom.Show(WorkingDayMargin.Around(working, DayRange));
        }
    }

    private bool HasWorkingDay() => WorkingDay is not null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ZoomToSelection()
    {
        if (Selection.Range is { } selection)
        {
            Zoom.Show(selection);
        }
    }

    /// <summary>
    /// Keyboard zoom keeps the now line in place when it's visible, and the middle of the view otherwise.
    /// </summary>
    private double ZoomAnchorX()
    {
        var viewport = Zoom.Viewport;
        var nowX = viewport.XOf(NowUtc);
        return nowX >= 0 && nowX <= viewport.Width ? nowX : viewport.Width / 2;
    }

    /// <summary>
    /// The lane showing <paramref name="kind"/>, once the day has loaded.
    /// </summary>
    private TimelineLane? Lane(LaneKind kind) => Lanes.FirstOrDefault(l => l.Kind == kind);

    private bool HasSelection() => Selection.HasSelection;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ClearSelection() => Selection.Clear();

    /// <summary>
    /// Finds the selection's edge within <see cref="EdgeGrabPixels"/> of <paramref name="time"/> and returns the
    /// opposite end, which stays put while that edge is dragged.
    /// It returns null when no edge is near.
    /// </summary>
    private DateTime? FindEdge(DateTime time) =>
        Selection.Range is { } selection ? BlockEdges.AnchorNear(selection, time, Pixels(EdgeGrabPixels)) : null;

    private DateTime SnapIf(bool snap, DateTime time) => Clamp(snap ? SnapToEdge(time) : time);

    private DateTime SnapToEdge(DateTime time) => BlockEdges.Snap(Lanes, time, Pixels(SnapDistancePixels));

    private TimeSpan Pixels(double pixels) => TimeSpan.FromSeconds(pixels * Zoom.Viewport.SecondsPerPixel);

    private DateTime Clamp(DateTime time) => TimeMath.Max(DayRange.Start, TimeMath.Min(time, DayRange.End));

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
