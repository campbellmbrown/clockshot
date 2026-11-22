using ClockShot.App.ViewModels.Timesheets;
using ClockShot.Core.Settings;
using ClockShot.Core.Timesheets;

namespace ClockShot.App.Tests.ViewModels.Timesheets;

public sealed class RoundingChoiceTests
{
    [Fact]
    public void NoRoundingKeepsTheSavedStep()
    {
        // Arrange
        var settings = new TimesheetSettings { Rounding = RoundingMode.Up, RoundingMinutes = 6 };
        var noRounding = RoundingChoice.All.Single(choice => choice.Mode == RoundingMode.None);

        // Act
        var changed = noRounding.ApplyTo(settings);

        // Assert
        Assert.Equal(new TimesheetSettings { Rounding = RoundingMode.None, RoundingMinutes = 6 }, changed);
    }

    [Fact]
    public void ARoundingChoiceSavesItsStep()
    {
        // Arrange
        var settings = new TimesheetSettings { RoundingMinutes = 6 };
        var upTo30 = RoundingChoice.All.Single(choice => choice.Mode == RoundingMode.Up && choice.Minutes == 30);

        // Act
        var changed = upTo30.ApplyTo(settings);

        // Assert
        Assert.Equal(new TimesheetSettings { Rounding = RoundingMode.Up, RoundingMinutes = 30 }, changed);
    }

    [Fact]
    public void NoRoundingMatchesAnySavedStep()
    {
        // Arrange
        var settings = new TimesheetSettings { Rounding = RoundingMode.None, RoundingMinutes = 6 };

        // Act
        var choice = RoundingChoice.For(settings);

        // Assert
        Assert.Equal(RoundingMode.None, choice.Mode);
    }

    [Fact]
    public void AStepThatIsNotOfferedMatchesNoRounding()
    {
        // Arrange
        var settings = new TimesheetSettings { Rounding = RoundingMode.Nearest, RoundingMinutes = 7 };

        // Act
        var choice = RoundingChoice.For(settings);

        // Assert
        Assert.Equal(RoundingMode.None, choice.Mode);
    }
}
