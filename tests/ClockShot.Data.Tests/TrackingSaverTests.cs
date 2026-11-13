using ClockShot.Core.Models;
using ClockShot.Core.Tracking;
using ClockShot.Data.Sqlite;
using ClockShot.Data.Tracking;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClockShot.Data.Tests;

public sealed class TrackingSaverTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new("Editor.exe", @"C:\Editor.exe", "Editor", "notes.txt");
    private static readonly ForegroundWindow Browser = new("Browser.exe", @"C:\Browser.exe", "Browser", "Inbox");
    private static readonly ForegroundWindow Chat = new("TeamChat.exe", @"C:\TeamChat.exe", "Team Chat", "General");
    private static readonly TimeRange WholeDay = new(Start.Date, Start.Date.AddDays(1));
    // These tests never go away, so any idle time is still active.
    private static readonly TimeSpan NeverAway = TimeSpan.MaxValue;

    // These tests never sleep, so any gap between samples is still tracked.
    private static readonly TimeSpan NeverAsleep = TimeSpan.MaxValue;

    private readonly MemoryDatabase _data = new();
    private readonly SqliteConnection _connection;
    private readonly TrackingSaver _saver;
    private readonly ActivityTracker _tracker = new(NeverAsleep);

    public TrackingSaverTests()
    {
        _connection = _data.Database.Open();
        _saver = new TrackingSaver(_connection, NullLogger.Instance);
    }

    public void Dispose()
    {
        _connection.Dispose();
        _data.Dispose();
    }

    [Fact]
    public void SavesTheEndOfTheOpenActivityEvery15Seconds()
    {
        // Act
        for (var second = 0; second <= 20; second++)
        {
            Sample(second, Editor);
        }

        // Assert
        Assert.Equal(At(15), Assert.Single(Activities()).Range.End);
    }

    [Fact]
    public void ChangesThatFailedToSaveAreSavedWithTheNextOnes()
    {
        // Arrange
        Sample(0, Editor);
        BreakActivityInserts();
        Sample(1, Browser);
        FixActivityInserts();

        // Act
        Sample(2, Browser);
        _saver.Finish();

        // Assert
        Assert.Equal(
            [("Editor", new TimeRange(At(0), At(1))), ("Browser", new TimeRange(At(1), At(2)))],
            Activities().Select(a => (DisplayNameOf(a), a.Range)));
    }

    [Fact]
    public void GivesUpOnChangesThatKeepFailingAndSavesLaterOnes()
    {
        // Arrange
        Sample(0, Editor);
        BreakActivityInserts();
        Sample(1, Browser);
        for (var second = 2; second <= TrackingSaver.GiveUpAfterFailedSaves; second++)
        {
            Sample(second, Browser);
        }

        FixActivityInserts();

        // Act
        Sample(TrackingSaver.GiveUpAfterFailedSaves + 1, Chat);
        _saver.Finish();

        // Assert
        Assert.Equal(["Editor", "Team Chat"], Activities().Select(DisplayNameOf));
    }

    private static DateTime At(int seconds) => Start.AddSeconds(seconds);

    private void Sample(int second, ForegroundWindow window)
    {
        var sample = new TrackingSample(At(second), window, IdleTime: TimeSpan.Zero, IsLocked: false);
        _saver.Save(_tracker.Process(sample, NeverAway));
    }

    private IReadOnlyList<ActivitySegment> Activities() => TrackingTables.GetActivities(_connection, WholeDay);

    private string DisplayNameOf(ActivitySegment activity) =>
        TrackingTables.GetApplications(_connection).Single(a => a.Id == activity.ApplicationId).DisplayName;

    // A trigger makes every new activity fail to save, the way a full or locked database would.
    private void BreakActivityInserts() => _connection.Execute(
        """
        CREATE TRIGGER fail_activity_insert BEFORE INSERT ON activity_segment
        BEGIN SELECT RAISE(ABORT, 'The test made saving fail.'); END
        """);

    private void FixActivityInserts() => _connection.Execute("DROP TRIGGER fail_activity_insert");
}
