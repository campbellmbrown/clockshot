using ClockShot.Core.Models;
using ClockShot.Core.Tracking;
using ClockShot.Data.Sqlite;
using ClockShot.Data.Tracking;

namespace ClockShot.Data.Tests;

public sealed class ActivityHistoryTests
{
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new(
        Handle: 1,
        ProcessName: "Editor.exe",
        ExecutablePath: @"C:\Editor.exe",
        DisplayName: "Editor",
        Title: "notes.txt");

    [Fact]
    public async Task LoadsSegmentsAndApplicationsInRange()
    {
        // Arrange
        using var data = new MemoryDatabase();
        using (var connection = data.Database.Open())
        {
            await using (var transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken))
            {
                var editorId = TrackingTables.GetOrAddApplication(connection, Editor, transaction);
                var activityId = TrackingTables.InsertActivity(connection, Nine, editorId, Editor.Title, transaction);
                TrackingTables.SetActivityEnd(connection, activityId, Nine.AddMinutes(1), transaction);
                var stateId = TrackingTables.InsertState(connection, Nine, ComputerState.Active, transaction);
                TrackingTables.SetStateEnd(connection, stateId, Nine.AddMinutes(1), transaction);
                await transaction.CommitAsync(TestContext.Current.CancellationToken);
            }
        }

        // Act
        var records = await new ActivityHistory(data.Database)
            .LoadAsync(new TimeRange(Nine, Nine.AddHours(1)), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new TimeRange(Nine, Nine.AddMinutes(1)), Assert.Single(records.Activities).Range);
        Assert.Equal(ComputerState.Active, Assert.Single(records.States).State);
        Assert.Equal("Editor", Assert.Single(records.Applications).Value.DisplayName);
    }
}
