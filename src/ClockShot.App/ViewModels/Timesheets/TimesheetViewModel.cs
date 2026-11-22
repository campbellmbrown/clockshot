using ClockShot.App.Controls;
using ClockShot.App.Services;
using ClockShot.Core.Settings;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Core.Timesheets;
using ClockShot.Data.Projects;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Timesheets;

/// <summary>
/// The Timesheet tab shows the tagged time in a range of days, by client, project, sub-project and task.
/// Each column is a day, or a week for long ranges.
/// The time can be rounded.
/// </summary>
public sealed partial class TimesheetViewModel : PageViewModel, IDisposable
{
    private readonly ITimeEntryRepository _entries;
    private readonly ProjectRepository _projects;
    private readonly ISettingsStore _settings;
    private readonly DayClock _days;
    private readonly BackgroundWork _background;
    private readonly NewestLoad _newestLoad = new();
    private List<TimesheetRowViewModel> _allRows = [];

    // Set while the page changes both dates itself, so neither change counts as a date picked by hand.
    private bool _isSettingDates;

    public TimesheetViewModel(
        ITimeEntryRepository entries,
        ProjectRepository projects,
        Tagger tagger,
        ISettingsStore settings,
        DayClock days,
        BackgroundWork background)
        : base("Timesheet", Icons.Timesheet)
    {
        _entries = entries;
        _projects = projects;
        _settings = settings;
        _days = days;
        _background = background;

        SelectedRounding = RoundingChoice.For(settings.Current.Timesheet);

        // Rounding can change here or on the Settings tab, and a new day start hour moves the days.
        settings.Changed += s => OnUiThread(() =>
        {
            SelectedRounding = RoundingChoice.For(s.Timesheet);
            _background.Run(() => LoadAsync());
        });
        SelectedPeriod = DatePeriod.ThisWeek;

        tagger.Changed += () => OnUiThread(() => _background.Run(() => LoadAsync()));
        projects.Changed += () => OnUiThread(() => _background.Run(() => LoadAsync()));
    }

    // ---- Range ----

    // All time has no first or last day, so it isn't offered.
    public IReadOnlyList<DatePeriod> Periods { get; } =
    [
        DatePeriod.Today,
        DatePeriod.Yesterday,
        DatePeriod.ThisWeek,
        DatePeriod.LastWeek,
        DatePeriod.ThisMonth,
        DatePeriod.LastMonth,
    ];

    /// <summary>
    /// The chosen period, or null when the dates were picked by hand.
    /// </summary>
    [ObservableProperty]
    public partial DatePeriod? SelectedPeriod { get; set; }

    /// <summary>
    /// The first day included, for the date button.
    /// </summary>
    [ObservableProperty]
    public partial DateTime? FromDate { get; set; }

    /// <summary>
    /// The last day included, for the date button.
    /// </summary>
    [ObservableProperty]
    public partial DateTime? ToDate { get; set; }

    partial void OnSelectedPeriodChanged(DatePeriod? value)
    {
        if (value?.DaysFrom(_days.Today) is not { } days)
        {
            return;
        }

        var (from, to) = days;
        SetDates(from.ToDateTime(TimeOnly.MinValue), to.ToDateTime(TimeOnly.MinValue));
        _background.Run(() => LoadAsync());
    }

    partial void OnFromDateChanged(DateTime? value) => OnDatesPicked();

    partial void OnToDateChanged(DateTime? value) => OnDatesPicked();

    /// <summary>
    /// Picking a date by hand clears the period.
    /// The period box then shows Custom.
    /// If the first day is after the last, they swap.
    /// </summary>
    private void OnDatesPicked()
    {
        if (_isSettingDates)
        {
            return;
        }

        if (FromDate > ToDate)
        {
            SetDates(ToDate, FromDate);
        }

        SelectedPeriod = null;
        _background.Run(() => LoadAsync());
    }

    private void SetDates(DateTime? from, DateTime? to)
    {
        _isSettingDates = true;
        FromDate = from;
        ToDate = to;
        _isSettingDates = false;
    }

    // ---- Rounding ----

    public IReadOnlyList<RoundingChoice> RoundingChoices { get; } = RoundingChoice.All;

    /// <summary>
    /// How each task-and-day cell is rounded.
    /// It's remembered in settings.
    /// </summary>
    [ObservableProperty]
    public partial RoundingChoice SelectedRounding { get; set; }

    // Saving raises the settings Changed event, whose handler reloads the timesheet.
    partial void OnSelectedRoundingChanged(RoundingChoice value)
    {
        var current = _settings.Current;
        var timesheet = value.ApplyTo(current.Timesheet);
        if (timesheet != current.Timesheet)
        {
            _settings.Save(current with { Timesheet = timesheet });
        }
    }

    // ---- Report ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ColumnLabels), nameof(ColumnTotals), nameof(TotalText), nameof(IsEmpty))]
    public partial TimesheetReport? Report { get; private set; }

    public IReadOnlyList<string> ColumnLabels => [.. Report?.Columns.Select(c => c.Label) ?? []];

    public IReadOnlyList<string> ColumnTotals => [.. Report?.ColumnTotals.Select(TimesheetRowViewModel.Format) ?? []];

    public string TotalText => Report is { } report ? TimesheetRowViewModel.Format(report.Total) : "";

    public bool IsEmpty => Report?.IsEmpty != false;

    /// <summary>
    /// The rows shown now, which leaves out the rows inside a collapsed row.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<TimesheetRowViewModel> VisibleRows { get; private set; } = [];

    /// <summary>
    /// Loads the tagged time for the range.
    /// A newer load cancels an unfinished older one.
    /// </summary>
    public async Task LoadAsync()
    {
        if (FromDate is not { } fromDate || ToDate is not { } toDate)
        {
            return;
        }

        var token = _newestLoad.Start();

        var from = DateOnly.FromDateTime(fromDate);
        var to = DateOnly.FromDateTime(toDate);
        var range = _days.RangeOf(from, to);

        try
        {
            var entries = await _entries.GetAsync(range, token);
            var catalog = await _projects.LoadCatalogAsync(token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            // The rounding box can't show a hand-edited step, so the timesheet uses the rounding the box shows.
            var rounding = SelectedRounding.ApplyTo(_settings.Current.Timesheet);
            Report = TimesheetBuilder.Build(entries, catalog, (from, to), _days, rounding);
            _allRows = [.. Flatten(Report.Rows, parent: null, depth: 0)];
            RefreshVisibleRows();
        }
        catch (OperationCanceledException)
        {
            // A newer load took over.
        }
    }

    private void RefreshVisibleRows() => VisibleRows = [.. _allRows.Where(r => r.IsShown)];

    private IEnumerable<TimesheetRowViewModel> Flatten(IEnumerable<TimesheetRow> rows, TimesheetRowViewModel? parent, int depth)
    {
        foreach (var row in rows)
        {
            var item = new TimesheetRowViewModel(row, parent, depth);
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TimesheetRowViewModel.IsExpanded))
                {
                    RefreshVisibleRows();
                }
            };
            yield return item;
            foreach (var child in Flatten(row.Children, item, depth + 1))
            {
                yield return child;
            }
        }
    }

    public void Dispose()
    {
        _newestLoad.Dispose();
    }
}
