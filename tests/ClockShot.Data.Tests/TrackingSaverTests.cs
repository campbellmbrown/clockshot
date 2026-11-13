using ClockShot.Core.Models;
using ClockShot.Core.Tracking;
using ClockShot.Data.Sqlite;
using ClockShot.Data.Tracking;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClockShot.Data.Tests;

public sealed class TrackingSaverTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new("Editor.exe", @"C:\Editor.exe", "Editor", "notes.txt");
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

    private static DateTime At(int seconds) => Start.AddSeconds(seconds);

    private void Sample(int second, ForegroundWindow window)
    {
        var sample = new TrackingSample(At(second), window, IdleTime: TimeSpan.Zero, IsLocked: false);
        _saver.Save(_tracker.Process(sample, NeverAway));
    }

    private IReadOnlyList<ActivitySegment> Activities() => TrackingTables.GetActivities(_connection, WholeDay);
}
