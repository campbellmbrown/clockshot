using ClockShot.Core.Models;
using ClockShot.Core.Settings;
using ClockShot.Core.Tracking;

namespace ClockShot.Core.Tests.Tracking;

public sealed class AwayPromptPolicyTests
{
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TrackingSettings AskAfterTenMinutes = new() { AwayPromptAfterMinutes = 10 };

    [Fact]
    public void AsksAfterALongEnoughAbsence()
    {
        // Act
        var shouldAsk = AwayPromptPolicy.ShouldAsk(AskAfterTenMinutes, AwayFor(minutes: 45), isPromptOpen: false);

        // Assert
        Assert.True(shouldAsk);
    }

    [Fact]
    public void AsksAfterExactlyTheMinimum()
    {
        // Act
        var shouldAsk = AwayPromptPolicy.ShouldAsk(AskAfterTenMinutes, AwayFor(minutes: 10), isPromptOpen: false);

        // Assert
        Assert.True(shouldAsk);
    }

    [Fact]
    public void DoesNotAskAfterAShortAbsence()
    {
        // Act
        var shouldAsk = AwayPromptPolicy.ShouldAsk(AskAfterTenMinutes, AwayFor(minutes: 9), isPromptOpen: false);

        // Assert
        Assert.False(shouldAsk);
    }

    [Fact]
    public void DoesNotAskWhenTurnedOff()
    {
        // Arrange
        var turnedOff = AskAfterTenMinutes with { AwayPromptEnabled = false };

        // Act
        var shouldAsk = AwayPromptPolicy.ShouldAsk(turnedOff, AwayFor(minutes: 45), isPromptOpen: false);

        // Assert
        Assert.False(shouldAsk);
    }

    [Fact]
    public void DoesNotAskWhileAPromptIsOpen()
    {
        // Act
        var shouldAsk = AwayPromptPolicy.ShouldAsk(AskAfterTenMinutes, AwayFor(minutes: 45), isPromptOpen: true);

        // Assert
        Assert.False(shouldAsk);
    }

    private static TimeRange AwayFor(int minutes) => new(Nine, Nine.AddMinutes(minutes));
}
