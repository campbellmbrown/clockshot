using System.IO.Abstractions.TestingHelpers;

using ClockShot.App.Services;
using ClockShot.App.ViewModels.Timesheets;
using ClockShot.Core.Models;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Data;
using ClockShot.Data.Projects;
using ClockShot.Data.Settings;
using ClockShot.Data.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

namespace ClockShot.App.Tests.ViewModels.Timesheets;

public sealed class TimesheetViewModelTests : IDisposable
{
    private readonly MemoryDatabase _database = new();
    private readonly ProjectRepository _projects;
    private readonly JsonSettingsStore _settings = new(new MockFileSystem(), new DataPaths("data"), NullLogger<JsonSettingsStore>.Instance);
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly DayClock _days;
    private readonly IFileSaveService _files = Substitute.For<IFileSaveService>();

    // A substitute, because the tests count how often the timesheet loads and choose what it loads.
    private readonly ITimeEntryRepository _entries = Substitute.For<ITimeEntryRepository>();

    public TimesheetViewModelTests()
    {
        _projects = new ProjectRepository(_database.Database);
        _days = new DayClock(_clock, _settings);
    }

    [Fact]
    public void PickingAStartAfterTheEndSwapsThemAndLoadsOnce()
    {
        // Arrange
        _entries.GetAsync(Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<IReadOnlyList<TimeEntry>>().Task);
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

    [Fact]
    public async Task AFailedSaveShowsWhy()
    {
        // Arrange
        using var timesheet = await LoadedTimesheetAsync();
        _files.SaveTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new IOException("The file is in use.")));

        // Act
        await timesheet.SaveCsvCommand.ExecuteAsync(parameter: null);

        // Assert
        Assert.Equal("Couldn't save the file: The file is in use.", timesheet.SaveError);
    }

    [Fact]
    public async Task ASaveAfterAFailedOneClearsTheError()
    {
        // Arrange
        using var timesheet = await LoadedTimesheetAsync();
        _files.SaveTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new IOException("The file is in use.")), Task.CompletedTask);
        await timesheet.SaveCsvCommand.ExecuteAsync(parameter: null);

        // Act
        await timesheet.SaveCsvCommand.ExecuteAsync(parameter: null);

        // Assert
        Assert.Equal("", timesheet.SaveError);
    }

    [Fact]
    public void ShowingTheTabOnALaterDayMovesThePeriodToThatDay()
    {
        // Arrange
        _entries.GetAsync(Arg.Any<TimeRange>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<IReadOnlyList<TimeEntry>>().Task);
        using var timesheet = Timesheet();
        timesheet.SelectedPeriod = DatePeriod.Today;
        _clock.Advance(TimeSpan.FromDays(1));

        // Act
        timesheet.OnShown();

        // Assert
        var nextDay = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal(nextDay, timesheet.FromDate);
        Assert.Equal(nextDay, timesheet.ToDate);
        Assert.Equal(DatePeriod.Today, timesheet.SelectedPeriod);
    }

    public void Dispose() => _database.Dispose();

    /// <summary>
    /// A timesheet showing an hour of tagged time on the first day of this week.
    /// </summary>
    private async Task<TimesheetViewModel> LoadedTimesheetAsync()
    {
        // Every load, including the ones the timesheet starts itself, finds the same hour.
        var project = await _projects.AddProjectAsync("Website", clientId: null, parentId: null);
        var monday = DatePeriod.ThisWeek.DaysFrom(_days.Today)!.Value.First;
        var start = _days.RangeOf(monday).Start;
        TimeEntry[] entries = [new(Id: 1, new TimeRange(start, start.AddHours(1)), project.Id, TaskId: null, Note: null)];
        _entries.GetAsync(Arg.Any<TimeRange>(), Arg.Any<CancellationToken>()).Returns(entries);

        var timesheet = Timesheet();
        await timesheet.LoadAsync();
        return timesheet;
    }

    private TimesheetViewModel Timesheet() => new(
        _entries,
        _projects,
        new Tagger(_entries),
        _settings,
        _days,
        Substitute.For<IClipboardService>(),
        _files,
        new BackgroundWork(NullLogger<BackgroundWork>.Instance));
}
