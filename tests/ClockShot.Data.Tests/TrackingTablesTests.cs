using ClockShot.Core.Models;
using ClockShot.Core.Tracking;
using ClockShot.Data.Sqlite;

using Microsoft.Data.Sqlite;

namespace ClockShot.Data.Tests;

public sealed class TrackingTablesTests : IDisposable
{
    private static readonly ForegroundWindow Editor = new(
        Handle: 1,
        ProcessName: "Editor.exe",
        ExecutablePath: @"C:\Editor.exe",
        DisplayName: "Editor",
        Title: "notes.txt");

    private static readonly TimeRange WholeDay = new(At(0), At(24));

    private readonly MemoryDatabase _data = new();
    private readonly SqliteConnection _connection;

    public TrackingTablesTests()
    {
        _connection = _data.Database.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _data.Dispose();
    }

    [Fact]
    public void ANewApplicationIsSavedWithTheColourOfItsName()
    {
        // Act
        var id = GetOrAddApplication(Editor);

        // Assert
        var expected = new TrackedApplication(id, "Editor.exe", @"C:\Editor.exe", "Editor", NameColor.For("Editor"));
        Assert.Equal(expected, Assert.Single(TrackingTables.GetApplications(_connection)));
    }

    [Fact]
    public void AnApplicationIsFoundAgainIgnoringCase()
    {
        // Arrange
        var firstId = GetOrAddApplication(Editor);
        var editorInCapitals = Editor with { ProcessName = "EDITOR.EXE", ExecutablePath = @"C:\EDITOR.EXE" };

        // Act
        var secondId = GetOrAddApplication(editorInCapitals);

        // Assert
        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public void AnActivityIsReadBackWithItsEnd()
    {
        // Arrange
        var range = new TimeRange(At(9), At(10));
        AddActivity(range);

        // Act
        var activities = TrackingTables.GetActivities(_connection, WholeDay);

        // Assert
        var activity = Assert.Single(activities);
        Assert.Equal(range, activity.Range);
        Assert.Equal("notes.txt", activity.WindowTitle);
    }

    [Theory]
    [InlineData(8, 9, false)]
    [InlineData(8, 10, true)]
    [InlineData(10, 12, true)]
    [InlineData(11, 12, false)]
    public void AnActivityIsReadOnlyWhenItOverlapsTheRange(int fromHour, int toHour, bool isRead)
    {
        // Arrange
        AddActivity(new TimeRange(At(9), At(11)));

        // Act
        var activities = TrackingTables.GetActivities(_connection, new TimeRange(At(fromHour), At(toHour)));

        // Assert
        Assert.Equal(isRead, activities.Count == 1);
    }

    [Fact]
    public void AStateIsReadBackWithItsEnd()
    {
        // Arrange
        var range = new TimeRange(At(9), At(10));
        AddState(ComputerState.Away, range);

        // Act
        var states = TrackingTables.GetStates(_connection, WholeDay);

        // Assert
        var state = Assert.Single(states);
        Assert.Equal(range, state.Range);
        Assert.Equal(ComputerState.Away, state.State);
    }

    [Theory]
    [InlineData(8, 9, false)]
    [InlineData(8, 10, true)]
    [InlineData(10, 12, true)]
    [InlineData(11, 12, false)]
    public void AStateIsReadOnlyWhenItOverlapsTheRange(int fromHour, int toHour, bool isRead)
    {
        // Arrange
        AddState(ComputerState.Active, new TimeRange(At(9), At(11)));

        // Act
        var states = TrackingTables.GetStates(_connection, new TimeRange(At(fromHour), At(toHour)));

        // Assert
        Assert.Equal(isRead, states.Count == 1);
    }

    private static DateTime At(int hour) => new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc).AddHours(hour);

    private long GetOrAddApplication(ForegroundWindow window)
    {
        using var transaction = _connection.BeginTransaction();
        var id = TrackingTables.GetOrAddApplication(_connection, window, transaction);
        transaction.Commit();
        return id;
    }

    private void AddActivity(TimeRange range)
    {
        var applicationId = GetOrAddApplication(Editor);
        using var transaction = _connection.BeginTransaction();
        var id = TrackingTables.InsertActivity(_connection, range.Start, applicationId, Editor.Title, transaction);
        TrackingTables.SetActivityEnd(_connection, id, range.End, transaction);
        transaction.Commit();
    }

    private void AddState(ComputerState state, TimeRange range)
    {
        using var transaction = _connection.BeginTransaction();
        var id = TrackingTables.InsertState(_connection, range.Start, state, transaction);
        TrackingTables.SetStateEnd(_connection, id, range.End, transaction);
        transaction.Commit();
    }
}
