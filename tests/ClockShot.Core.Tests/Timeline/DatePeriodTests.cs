using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class DatePeriodTests
{
    private static readonly DateOnly Wednesday = new(2026, 11, 11);
    private static readonly DateOnly Monday = new(2026, 11, 9);
    private static readonly DateOnly Sunday = new(2026, 11, 15);

    [Fact]
    public void TodayAndYesterdayAreOneDayEach()
    {
        // Act
        var today = DatePeriod.Today.DaysFrom(Wednesday);
        var yesterday = DatePeriod.Yesterday.DaysFrom(Wednesday);

        // Assert
        Assert.Equal((Wednesday, Wednesday), today);
        Assert.Equal((Wednesday.AddDays(-1), Wednesday.AddDays(-1)), yesterday);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(15)]
    public void AWeekRunsFromMondayToSunday(int dayOfNovember)
    {
        // Arrange
        var today = new DateOnly(2026, 11, dayOfNovember);

        // Act
        var thisWeek = DatePeriod.ThisWeek.DaysFrom(today);
        var lastWeek = DatePeriod.LastWeek.DaysFrom(today);

        // Assert
        Assert.Equal((Monday, Sunday), thisWeek);
        Assert.Equal((Monday.AddDays(-7), Sunday.AddDays(-7)), lastWeek);
    }

    [Fact]
    public void AMonthRunsFromItsFirstDayToItsLast()
    {
        // Act
        var thisMonth = DatePeriod.ThisMonth.DaysFrom(Wednesday);
        var lastMonth = DatePeriod.LastMonth.DaysFrom(Wednesday);

        // Assert
        Assert.Equal((new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)), thisMonth);
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), lastMonth);
    }

    [Fact]
    public void LastMonthInJanuaryIsTheDecemberBefore()
    {
        // Act
        var lastMonth = DatePeriod.LastMonth.DaysFrom(new DateOnly(2027, 1, 20));

        // Assert
        Assert.Equal((new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)), lastMonth);
    }

    [Fact]
    public void LastMonthFromTheEndOfMarchIsAllOfALeapFebruary()
    {
        // Act
        var lastMonth = DatePeriod.LastMonth.DaysFrom(new DateOnly(2028, 3, 31));

        // Assert
        Assert.Equal((new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29)), lastMonth);
    }

    [Fact]
    public void AllTimeHasNoDays()
    {
        // Act
        var allTime = DatePeriod.AllTime.DaysFrom(Wednesday);

        // Assert
        Assert.Null(allTime);
    }

    [Fact]
    public void EachPeriodHasItsLabel()
    {
        // Act
        string[] labels =
        [
            DatePeriod.Today.Label,
            DatePeriod.Yesterday.Label,
            DatePeriod.ThisWeek.Label,
            DatePeriod.LastWeek.Label,
            DatePeriod.ThisMonth.Label,
            DatePeriod.LastMonth.Label,
            DatePeriod.AllTime.Label,
        ];

        // Assert
        Assert.Equal(["Today", "Yesterday", "This week", "Last week", "This month", "Last month", "All time"], labels);
    }
}
