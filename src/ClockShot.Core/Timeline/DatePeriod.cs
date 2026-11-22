namespace ClockShot.Core.Timeline;

/// <summary>
/// A named run of days, such as "Last week", worked out from today.
/// </summary>
public sealed class DatePeriod
{
    private const int DaysInWeek = 7;

    private readonly Func<DateOnly, (DateOnly First, DateOnly Last)>? _days;

    private DatePeriod(string label, Func<DateOnly, (DateOnly First, DateOnly Last)>? days)
    {
        Label = label;
        _days = days;
    }

    public static DatePeriod Today { get; } = new("Today", today => (today, today));
    public static DatePeriod Yesterday { get; } = new("Yesterday", today => (today.AddDays(-1), today.AddDays(-1)));
    public static DatePeriod ThisWeek { get; } = new("This week", WeekOf);
    public static DatePeriod LastWeek { get; } = new("Last week", today => WeekOf(today.AddDays(-DaysInWeek)));
    public static DatePeriod ThisMonth { get; } = new("This month", MonthOf);
    public static DatePeriod LastMonth { get; } = new("Last month", today => MonthOf(today.AddMonths(-1)));

    /// <summary>
    /// Every day there is.
    /// It has no first or last day.
    /// </summary>
    public static DatePeriod AllTime { get; } = new("All time", days: null);

    public string Label { get; }

    /// <summary>
    /// The period's first and last day, both included, or null for All time.
    /// </summary>
    public (DateOnly First, DateOnly Last)? DaysFrom(DateOnly today) => _days?.Invoke(today);

    /// <summary>
    /// The Monday to Sunday week that holds the day, whatever the user's locale.
    /// </summary>
    public static (DateOnly First, DateOnly Last) WeekOf(DateOnly day)
    {
        var daysSinceMonday = ((int)day.DayOfWeek - (int)DayOfWeek.Monday + DaysInWeek) % DaysInWeek;
        var monday = day.AddDays(-daysSinceMonday);
        return (monday, monday.AddDays(DaysInWeek - 1));
    }

    private static (DateOnly First, DateOnly Last) MonthOf(DateOnly day)
    {
        var first = new DateOnly(day.Year, day.Month, 1);
        return (first, first.AddMonths(1).AddDays(-1));
    }
}
