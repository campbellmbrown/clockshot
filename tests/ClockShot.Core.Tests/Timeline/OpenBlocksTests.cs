using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class OpenBlocksTests
{
    // OpenBlocks never reads ids, so every record shares one.
    private const long AnyId = 1;

    private static readonly DateTime Start = new(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan SavedWithin = TimeSpan.FromSeconds(45);

    [Fact]
    public void RecentlySavedBlocksReachNow()
    {
        // Arrange
        var records = Records(ComputerState.Active, end: At(10));

        // Act
        var result = OpenBlocks.UpTo(records, nowUtc: At(10, 30), SavedWithin);

        // Assert
        Assert.Equal(At(10, 30), result.States[^1].Range.End);
        Assert.Equal(At(10, 30), result.Activities[^1].Range.End);
    }

    [Fact]
    public void BlocksSavedLongAgoAreLeftAlone()
    {
        // Arrange
        var records = Records(ComputerState.Active, end: At(10));

        // Act
        var result = OpenBlocks.UpTo(records, nowUtc: At(12), SavedWithin);

        // Assert
        Assert.Same(records, result);
    }

    [Fact]
    public void WhileAwayOnlyTheStateReachesNow()
    {
        // Arrange
        var records = Records(ComputerState.Away, end: At(10));

        // Act
        var result = OpenBlocks.UpTo(records, nowUtc: At(10, 30), SavedWithin);

        // Assert
        Assert.Equal(At(10, 30), result.States[^1].Range.End);
        Assert.Equal(At(10), result.Activities[^1].Range.End);
    }

    [Fact]
    public void AWindowFromBeforeALockIsNotStretchedAcrossIt()
    {
        // Arrange
        // The screen was locked for 20 seconds, then unlocked to no window.
        // The window ended within SavedWithin of now, so only the lock keeps it from being stretched.
        var records = new ActivityRecords(
            [Activity(new TimeRange(Start, At(10)))],
            [
                State(new TimeRange(Start, At(10)), ComputerState.Active),
                State(new TimeRange(At(10), At(10, 20)), ComputerState.Locked),
                State(new TimeRange(At(10, 20), At(10, 30)), ComputerState.Active),
            ],
            new Dictionary<long, TrackedApplication>(),
            []);

        // Act
        var result = OpenBlocks.UpTo(records, nowUtc: At(10, 40), SavedWithin);

        // Assert
        Assert.Equal(At(10, 40), result.States[^1].Range.End);
        Assert.Equal(At(10), result.Activities[^1].Range.End);
    }

    [Fact]
    public void AWindowThatStartedWithTheStateIsStretched()
    {
        // Arrange
        // Just after an unlock, the state and the window start together and haven't been saved since.
        var records = Records(ComputerState.Active, end: Start);

        // Act
        var result = OpenBlocks.UpTo(records, nowUtc: At(0, 10), SavedWithin);

        // Assert
        Assert.Equal(At(0, 10), result.Activities[^1].Range.End);
    }

    [Fact]
    public void NoRecordsStayEmpty()
    {
        // Act
        var result = OpenBlocks.UpTo(ActivityRecords.Empty, nowUtc: At(10), SavedWithin);

        // Assert
        Assert.Same(ActivityRecords.Empty, result);
    }

    private static DateTime At(int minutes, int seconds = 0) => Start.AddMinutes(minutes).AddSeconds(seconds);

    /// <summary>
    /// One window and one state, both from <see cref="Start"/> to <paramref name="end"/>.
    /// </summary>
    private static ActivityRecords Records(ComputerState state, DateTime end) => new(
        [Activity(new TimeRange(Start, end))],
        [State(new TimeRange(Start, end), state)],
        new Dictionary<long, TrackedApplication>(),
        []);

    private static ActivitySegment Activity(TimeRange range) =>
        new(AnyId, range, ApplicationId: AnyId, WindowTitle: "notes.txt");

    private static ComputerStateSegment State(TimeRange range, ComputerState state) => new(AnyId, range, state);
}
