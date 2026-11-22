using System.IO.Abstractions.TestingHelpers;

using ClockShot.App.Services;
using ClockShot.App.ViewModels.Timesheets;
using ClockShot.Core.Models;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Data;
using ClockShot.Data.Projects;
using ClockShot.Data.Settings;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace ClockShot.App.Tests.ViewModels.Timesheets;

public sealed class TimesheetViewModelTests
{
    // A substitute, because the test counts how often the timesheet loads.
    private readonly ITimeEntryRepository _entries = Substitute.For<ITimeEntryRepository>();

    public TimesheetViewModelTests()
    {
        // Loads never finish, so they never reach the projects.
        _entries.GetAsync(Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<IReadOnlyList<TimeEntry>>().Task);
    }

    [Fact]
    public void PickingAStartAfterTheEndSwapsThemAndLoadsOnce()
    {
        // Arrange
        using var timesheet = Timesheet();
        var end = timesheet.ToDate!.Value;
        _entries.ClearReceivedCalls();

        // Act
        timesheet.FromDate = end.AddDays(3);

        // Assert
        Assert.Equal(end, timesheet.FromDate);
        Assert.Equal(end.AddDays(3), timesheet.ToDate);
        Assert.Null(timesheet.SelectedPeriod);
        _entries.Received(1).GetAsync(Arg.Any<TimeRange>(), Arg.Any<CancellationToken>());
    }

    private TimesheetViewModel Timesheet()
    {
        // The test never loads projects, so the database is never opened.
        var database = UnopenedDatabase.Create();
        var settings = new JsonSettingsStore(new MockFileSystem(), new DataPaths("data"), NullLogger<JsonSettingsStore>.Instance);

        return new TimesheetViewModel(
            _entries,
            new ProjectRepository(database),
            new Tagger(_entries),
            new DayClock(TimeProvider.System, settings),
            new BackgroundWork(NullLogger<BackgroundWork>.Instance));
    }
}
