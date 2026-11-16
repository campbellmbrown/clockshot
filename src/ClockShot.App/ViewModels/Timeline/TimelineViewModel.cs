using ClockShot.App.Controls;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// The Timeline tab, which shows one day of recorded activity.
/// </summary>
public sealed partial class TimelineViewModel : PageViewModel
{
    private readonly DayClock _days;

    public TimelineViewModel(DayClock days)
        : base("Timeline", Icons.Timeline)
    {
        _days = days;

        Day = Today();
    }

    /// <summary>
    /// The day being shown.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDate))]
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

    [RelayCommand]
    private void PreviousDay() => Day = Day.AddDays(-1);

    [RelayCommand(CanExecute = nameof(CanGoToNextDay))]
    private void NextDay() => Day = Day.AddDays(1);

    private bool CanGoToNextDay() => Day < Today();

    [RelayCommand]
    private void GoToToday() => Day = Today();

    private DateOnly Today() => _days.Today;
}
