using ClockShot.Core.Settings;

namespace ClockShot.Core.Tests.Settings;

public sealed class AppSettingsTests
{
    [Fact]
    public void DefaultsAreAlreadyNormal()
    {
        // Arrange
        var defaults = new AppSettings();

        // Act
        var normalized = defaults.Normalized();

        // Assert
        Assert.Equal(defaults.General, normalized.General);
        Assert.Equal(defaults.Tracking, normalized.Tracking);
    }

    [Fact]
    public void NormalizedReplacesMissingSectionsWithDefaults()
    {
        // Arrange
        var settings = new AppSettings { General = null! };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Equal(new GeneralSettings(), normalized.General);
    }

    [Fact]
    public void NormalizedClampsEachNumberToItsLimits()
    {
        // Arrange
        var settings = new AppSettings
        {
            Tracking = new TrackingSettings { AwayAfterMinutes = 0 },
        };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Equal(SettingLimits.AwayAfterMinutes.Min, normalized.Tracking.AwayAfterMinutes);
    }
}
