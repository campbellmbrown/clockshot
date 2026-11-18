using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class ZoomStepsTests
{
    [Fact]
    public void LongerIsTheNextStepUp()
    {
        // Act
        var longer = ZoomSteps.Longer(TimeSpan.FromMinutes(40));

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), longer);
    }

    [Fact]
    public void ShorterIsTheNextStepDown()
    {
        // Act
        var shorter = ZoomSteps.Shorter(TimeSpan.FromMinutes(40));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), shorter);
    }

    [Fact]
    public void ALengthJustOffAStepCountsAsThatStep()
    {
        // Arrange
        var justUnderAnHour = TimeSpan.FromMinutes(59.7);
        var justOverAnHour = TimeSpan.FromMinutes(60.3);

        // Act
        var longer = ZoomSteps.Longer(justUnderAnHour);
        var shorter = ZoomSteps.Shorter(justOverAnHour);

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), longer);
        Assert.Equal(TimeSpan.FromMinutes(30), shorter);
    }

    [Fact]
    public void ThereIsNothingLongerThanAWholeDay()
    {
        // Act
        var longer = ZoomSteps.Longer(TimeSpan.FromDays(1));

        // Assert
        Assert.Null(longer);
    }

    [Fact]
    public void ThereIsNothingShorterThanAMinute()
    {
        // Act
        var shorter = ZoomSteps.Shorter(TimeSpan.FromMinutes(1));

        // Assert
        Assert.Null(shorter);
    }
}
