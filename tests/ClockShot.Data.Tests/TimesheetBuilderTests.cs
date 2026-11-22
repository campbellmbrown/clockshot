using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Settings;
using ClockShot.Core.Timeline;
using ClockShot.Core.Timesheets;

using Microsoft.Extensions.Time.Testing;

namespace ClockShot.Data.Tests;

public sealed class TimesheetBuilderTests
{
    private const long ClientAId = 1;
    private const long WebsiteId = 10;
    private const long CheckoutId = 11;
    private const long InternalId = 20;
    private const long UnusedId = 30;
    private const long LoginPageId = 100;

    private static readonly DateOnly Monday = new(2026, 11, 9);

    // The fake clock is in UTC and a day starts at midnight by default, so each day is a UTC calendar day.
    private static readonly DayClock Days = new(new FakeTimeProvider(), MemorySettingsStore.Create());

    private static readonly ProjectCatalog Catalog = new(
        [new Client(ClientAId, "Client A", IsArchived: false)],
        [
            new Project(WebsiteId, ClientAId, ParentId: null, "Website", IsArchived: false),
            new Project(CheckoutId, ClientId: null, WebsiteId, "Checkout", IsArchived: false),
            new Project(InternalId, ClientId: null, ParentId: null, "Internal", IsArchived: false),
            new Project(UnusedId, ClientId: null, ParentId: null, "Unused", IsArchived: false),
        ],
        [new ProjectTask(LoginPageId, WebsiteId, "Login page", IsArchived: false)]);

    [Fact]
    public void RowsFollowClientProjectSubProjectTaskAndLeaveOutEmptyOnes()
    {
        // Arrange
        TimeEntry[] entries =
        [
            Entry(Monday, startHour: 9, hours: 1, WebsiteId, LoginPageId),
            Entry(Monday, startHour: 10, hours: 0.5, WebsiteId),
            Entry(Monday.AddDays(1), startHour: 9, hours: 2, CheckoutId),
            Entry(Monday.AddDays(2), startHour: 13, hours: 1, InternalId),
        ];

        // Act
        var report = Build(entries, Monday, Monday.AddDays(6));

        // Assert
        Assert.Equal(["Client A", "No client"], report.Rows.Select(r => r.Name));
        var website = Assert.Single(report.Rows[0].Children);
        Assert.Equal(["Checkout", "General", "Login page"], website.Children.Select(c => c.Name));
        Assert.Equal(TimeSpan.FromHours(3.5), website.Total);
        Assert.Equal(["Internal"], report.Rows[1].Children.Select(c => c.Name));
        Assert.Equal(TimeSpan.FromHours(4.5), report.Total);
    }

    [Fact]
    public void ShortRangesHaveOneColumnPerDay()
    {
        // Arrange
        TimeEntry[] entries =
        [
            Entry(Monday, startHour: 9, hours: 1, InternalId),
            Entry(Monday.AddDays(2), startHour: 9, hours: 2, InternalId),
        ];

        // Act
        var report = Build(entries, Monday, Monday.AddDays(6));

        // Assert
        Assert.Equal(7, report.Columns.Count);
        Assert.Equal([TimeSpan.FromHours(1), TimeSpan.Zero, TimeSpan.FromHours(2)], report.ColumnTotals.Take(3));
    }

    [Fact]
    public void LongRangesHaveOneColumnPerWeekStartingMonday()
    {
        // Arrange
        TimeEntry[] entries = [Entry(Monday.AddDays(8), startHour: 9, hours: 1, InternalId)];

        // Act
        var report = Build(entries, new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31));

        // Assert
        Assert.Equal(new DateOnly(2026, 11, 1), report.Columns[0].From);
        Assert.Equal(new DateOnly(2026, 11, 1), report.Columns[0].To);   // Sunday 1 November is a week on its own.
        Assert.Equal((Monday, Monday.AddDays(6)), (report.Columns[2].From, report.Columns[2].To));
        Assert.Equal(TimeSpan.FromHours(1), report.ColumnTotals[3]);
    }

    [Fact]
    public void TagsCrossingMidnightAreSplitBetweenTheDays()
    {
        // Arrange
        TimeEntry[] entries = [Entry(Monday, startHour: 23, hours: 2, InternalId)];

        // Act
        var report = Build(entries, Monday, Monday.AddDays(1));

        // Assert
        Assert.Equal([TimeSpan.FromHours(1), TimeSpan.FromHours(1)], report.ColumnTotals);
    }

    [Fact]
    public void TimeOutsideTheRangeIsLeftOut()
    {
        // Arrange
        TimeEntry[] entries = [Entry(Monday.AddDays(-1), startHour: 9, hours: 1, InternalId)];

        // Act
        var report = Build(entries, Monday, Monday.AddDays(6));

        // Assert
        Assert.True(report.IsEmpty);
    }

    [Fact]
    public void RoundingAppliesToEachCellAndTotalsAddUpTheRoundedCells()
    {
        // Arrange
        TimeEntry[] entries =
        [
            Entry(Monday, startHour: 9, hours: 7.0 / 60, InternalId),
            Entry(Monday.AddDays(1), startHour: 9, hours: 8.0 / 60, InternalId),
        ];

        // Act
        var nearest = Build(entries, Monday, Monday.AddDays(1), new TimesheetSettings { Rounding = RoundingMode.Nearest, RoundingMinutes = 15 });
        var up = Build(entries, Monday, Monday.AddDays(1), new TimesheetSettings { Rounding = RoundingMode.Up, RoundingMinutes = 15 });

        // Assert
        Assert.Equal([TimeSpan.Zero, TimeSpan.FromMinutes(15)], nearest.ColumnTotals);
        Assert.Equal(TimeSpan.FromMinutes(15), nearest.Total);
        Assert.Equal(TimeSpan.FromMinutes(30), up.Total);
    }

    [Theory]
    [InlineData(0, RoundingMode.Up, 6, 0)]
    [InlineData(1, RoundingMode.Up, 6, 6)]
    [InlineData(3, RoundingMode.Nearest, 6, 6)]
    [InlineData(2, RoundingMode.Nearest, 6, 0)]
    [InlineData(31, RoundingMode.Nearest, 30, 30)]
    [InlineData(31, RoundingMode.None, 15, 31)]
    public void RoundsToTheStep(int minutes, RoundingMode mode, int step, int expected)
    {
        // Arrange
        var settings = new TimesheetSettings { Rounding = mode, RoundingMinutes = step };

        // Act
        var rounded = TimesheetBuilder.Round(TimeSpan.FromMinutes(minutes), settings);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expected), rounded);
    }

    private static TimeEntry Entry(DateOnly day, int startHour, double hours, long project, long? task = null)
    {
        var start = day.ToDateTime(new TimeOnly(startHour, minute: 0), DateTimeKind.Utc);
        return new TimeEntry(Id: 0, new TimeRange(start, start.AddHours(hours)), project, task, Note: null);
    }

    private static TimesheetReport Build(IReadOnlyList<TimeEntry> entries, DateOnly first, DateOnly last, TimesheetSettings? settings = null) =>
        TimesheetBuilder.Build(entries, Catalog, (first, last), Days, settings ?? new TimesheetSettings());
}
