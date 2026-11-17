using ClockShot.App.Controls;
using ClockShot.App.Services;
using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// The Timeline tab, which shows one day of recorded activity.
/// </summary>
public sealed partial class TimelineViewModel : PageViewModel, IDisposable
{
    private readonly IActivityHistory _history;
    private readonly DayClock _days;
    private readonly ILogger<TimelineViewModel> _logger;
    private readonly BackgroundWork _background;

    private ActivityRecords _records = ActivityRecords.Empty;
    private CancellationTokenSource? _loading;

    public TimelineViewModel(
        IActivityHistory history,
        DayClock days,
        ILogger<TimelineViewModel> logger,
        BackgroundWork background)
        : base("Timeline", Icons.Timeline)
    {
        _history = history;
        _days = days;
        _logger = logger;
        _background = background;

        Zoom = new TimelineZoom();

        Zoom.ViewportChanged += () => RedrawNeeded?.Invoke();

        // Setting the day loads it, through OnDayChanged.
        Day = Today();
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
    /// From the first to the last active time of the day, or null if the computer was never active.
    /// </summary>
    [ObservableProperty]
    public partial TimeRange? WorkingDay { get; private set; }

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
        _loading?.Cancel();
        _loading?.Dispose();
    }

    partial void OnDayChanged(DateOnly value)
    {
        Zoom.Fit(DayRange);
        _background.Run(() => LoadAsync());
    }

    [RelayCommand]
    private void PreviousDay() => Day = Day.AddDays(-1);

    [RelayCommand(CanExecute = nameof(CanGoToNextDay))]
    private void NextDay() => Day = Day.AddDays(1);

    private bool CanGoToNextDay() => Day < Today();

    [RelayCommand]
    private void GoToToday() => Day = Today();

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

    /// <summary>
    /// Builds the applications list and summary from the loaded records.
    /// </summary>
    private void ShowRecords()
    {
        var totals = ActivitySummary.ByApplication(_records.Activities, DayRange);
        DayApplications = ApplicationTotalViewModel.From(totals, _records.Applications);

        // Summarize reads WorkingDay, so it's set first.
        WorkingDay = ActivitySummary.WorkingDay(_records.States, DayRange);
        SummaryText = Summarize(_records);
    }

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
