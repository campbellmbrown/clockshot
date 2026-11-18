using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class WorkingDayMarginTests
{
    private static readonly DateTime Midnight = new(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeRange Day = new(Midnight, Midnight.AddDays(1));

    [Fact]
    public void AddsTwoPercentOfTheWorkingDayEitherSide()
    {
        // Arrange
        var workingDay = new TimeRange(Midnight.AddHours(8), Midnight.AddHours(18));

        // Act
        var shown = WorkingDayMargin.Around(workingDay, Day);

        // Assert
        var margin = TimeSpan.FromMinutes(12);
        Assert.Equal(new TimeRange(workingDay.Start - margin, workingDay.End + margin), shown);
    }

    [Fact]
    public void AShortWorkingDayGetsAtLeastAMinuteEitherSide()
    {
        // Arrange
        var workingDay = new TimeRange(Midnight.AddHours(8), Midnight.AddHours(8).AddMinutes(10));

        // Act
        var shown = WorkingDayMargin.Around(workingDay, Day);

        // Assert
        var margin = TimeSpan.FromMinutes(1);
        Assert.Equal(new TimeRange(workingDay.Start - margin, workingDay.End + margin), shown);
    }

    [Fact]
    public void TheMarginStaysInsideTheDay()
    {
        // Arrange
        var workingDay = new TimeRange(Midnight.AddMinutes(5), Day.End.AddMinutes(-5));

        // Act
        var shown = WorkingDayMargin.Around(workingDay, Day);

        // Assert
        Assert.Equal(Day, shown);
    }
}
