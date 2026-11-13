using ClockShot.Core.Models;
using ClockShot.Core.Tracking;
using ClockShot.Data.Sqlite;
using ClockShot.Data.Tracking;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClockShot.Data.Tests;

public sealed class TrackingWriterTests
{
    private static readonly DateTime Start = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new("Editor.exe", @"C:\Editor.exe", "Editor", "notes.txt");
    private static readonly ForegroundWindow Browser = new("Browser.exe", @"C:\Browser.exe", "Browser", "Inbox");
    private static readonly TimeRange WholeDay = new(Start.Date, Start.Date.AddDays(1));
    // These tests never go away, so any idle time is still active.
    private static readonly TimeSpan NeverAway = TimeSpan.MaxValue;

    // These tests never sleep, so any gap between samples is still tracked.
    private static readonly TimeSpan NeverAsleep = TimeSpan.MaxValue;

    private static DateTime At(int seconds) => Start.AddSeconds(seconds);

    private static TrackingSample ActiveSample(int second, ForegroundWindow window) =>
        new(At(second), window, IdleTime: TimeSpan.Zero, IsLocked: false);

    [Fact]
    public async Task SavesSegmentsProducedByTracker()
    {
        // Arrange
        using var data = new MemoryDatabase();
        var writer = new TrackingWriter(data.Database, NullLogger<TrackingWriter>.Instance);
        var tracker = new ActivityTracker(NeverAsleep);

        // Act
        for (var second = 0; second < 30; second++)
        {
            writer.Write(tracker.Process(ActiveSample(second, Editor), NeverAway));
        }

        for (var second = 30; second < 45; second++)
        {
            writer.Write(tracker.Process(ActiveSample(second, Browser), NeverAway));
        }

        writer.Write(tracker.Stop(At(45)));
        await writer.CompleteAsync();

        // Assert
        using var connection = data.Database.Open();
        var activities = TrackingTables.GetActivities(connection, WholeDay);
        var states = TrackingTables.GetStates(connection, WholeDay);
        var applications = TrackingTables.GetApplications(connection);

        Assert.Equal([new TimeRange(At(0), At(30)), new TimeRange(At(30), At(45))], activities.Select(a => a.Range));
        Assert.Equal(["notes.txt", "Inbox"], activities.Select(a => a.WindowTitle));
        Assert.Equal([(ComputerState.Active, new TimeRange(At(0), At(45)))], states.Select(s => (s.State, s.Range)));
        Assert.Equal(["Editor", "Browser"], applications.Select(a => a.DisplayName));
        Assert.All(applications, a => Assert.Matches("^#[0-9A-F]{6}$", a.ColorHex));
    }

    [Fact]
    public async Task OpenSegmentEndIsSavedOnComplete()
    {
        // Arrange
        using var data = new MemoryDatabase();
        var writer = new TrackingWriter(data.Database, NullLogger<TrackingWriter>.Instance);
        var tracker = new ActivityTracker(NeverAsleep);
        for (var second = 0; second <= 20; second++)
        {
            writer.Write(tracker.Process(ActiveSample(second, Editor), NeverAway));
        }

        // Act
        await writer.CompleteAsync();

        // Assert
        using var connection = data.Database.Open();
        Assert.Equal(At(20), Assert.Single(TrackingTables.GetActivities(connection, WholeDay)).Range.End);
    }

    [Fact]
    public async Task ReusesApplicationRowAcrossWriters()
    {
        // Arrange
        using var data = new MemoryDatabase();

        // Act
        foreach (var offset in new[] { 0, 100 })
        {
            var writer = new TrackingWriter(data.Database, NullLogger<TrackingWriter>.Instance);
            var tracker = new ActivityTracker(NeverAsleep);
            writer.Write(tracker.Process(ActiveSample(offset, Editor), NeverAway));
            writer.Write(tracker.Stop(At(offset + 5)));
            await writer.CompleteAsync();
        }

        // Assert
        using var connection = data.Database.Open();
        Assert.Single(TrackingTables.GetApplications(connection));
        Assert.Equal(2, TrackingTables.GetActivities(connection, WholeDay).Count);
    }

    [Fact]
    public async Task ReusesApplicationRowWhenTheNameChangesCaseAfterARestart()
    {
        // Arrange
        using var data = new MemoryDatabase();
        var editorInCapitals = Editor with { ProcessName = "EDITOR.EXE", ExecutablePath = @"C:\EDITOR.EXE" };

        // Act
        foreach (var (window, offset) in new[] { (Editor, 0), (editorInCapitals, 100) })
        {
            var writer = new TrackingWriter(data.Database, NullLogger<TrackingWriter>.Instance);
            var tracker = new ActivityTracker(NeverAsleep);
            writer.Write(tracker.Process(ActiveSample(offset, window), NeverAway));
            writer.Write(tracker.Stop(At(offset + 5)));
            await writer.CompleteAsync();
        }

        // Assert
        using var connection = data.Database.Open();
        Assert.Single(TrackingTables.GetApplications(connection));
        Assert.Equal(2, TrackingTables.GetActivities(connection, WholeDay).Count);
    }

    [Fact]
    public async Task ADatabaseThatCannotBeOpenedIsLoggedInsteadOfThrown()
    {
        // Arrange
        var missingFile = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.db"),
            // Without Create, SQLite refuses to open a file that doesn't exist.
            Mode = SqliteOpenMode.ReadWrite,
        };
        var writer = new TrackingWriter(new SqliteDatabase(missingFile), NullLogger<TrackingWriter>.Instance);
        var tracker = new ActivityTracker(NeverAsleep);
        writer.Start();
        writer.Write(tracker.Process(ActiveSample(0, Editor), NeverAway));

        // Act
        var exception = await Record.ExceptionAsync(writer.CompleteAsync);

        // Assert
        Assert.Null(exception);
    }
}
