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
        Assert.Equal(defaults.Screenshots, normalized.Screenshots);
        Assert.Equal(defaults.Layout, normalized.Layout);
    }

    [Fact]
    public void NormalizedReplacesMissingSectionsWithDefaults()
    {
        // Arrange
        var settings = new AppSettings { General = null!, Screenshots = null! };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Equal(new GeneralSettings(), normalized.General);
        Assert.Equal(new ScreenshotSettings(), normalized.Screenshots);
    }

    [Fact]
    public void NormalizedReplacesAMissingExcludedAppListWithAnEmptyOne()
    {
        // Arrange
        var settings = new AppSettings { Screenshots = new ScreenshotSettings { ExcludedProcessNames = null! } };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Empty(normalized.Screenshots.ExcludedProcessNames);
    }

    [Fact]
    public void NormalizedTrimsExcludedAppsAndDropsBlankOnes()
    {
        // Arrange
        var settings = new AppSettings
        {
            Screenshots = new ScreenshotSettings { ExcludedProcessNames = [" Editor.exe ", "", null!, "  "] },
        };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Equal(["Editor.exe"], normalized.Screenshots.ExcludedProcessNames);
    }

    [Fact]
    public void NormalizedShortensAGapLongerThanTheInterval()
    {
        // Arrange
        var settings = new AppSettings { Screenshots = new ScreenshotSettings { IntervalSeconds = 30, MinimumGapSeconds = 100 } };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Equal(30, normalized.Screenshots.MinimumGapSeconds);
    }

    [Fact]
    public void NormalizedTurnsNegativePanelSizesIntoZero()
    {
        // Arrange
        var settings = new AppSettings { Layout = new LayoutSettings { TimelineHeight = -50, DetailsPanelWidth = -1 } };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Equal(new LayoutSettings { TimelineHeight = 0, DetailsPanelWidth = 0 }, normalized.Layout);
    }

    [Fact]
    public void NormalizedClampsEachNumberToItsLimits()
    {
        // Arrange
        var settings = new AppSettings
        {
            General = new GeneralSettings { DayStartHour = 24 },
            Tracking = new TrackingSettings { AwayAfterMinutes = 0, AwayPromptAfterMinutes = -5 },
            Screenshots = new ScreenshotSettings
            {
                IntervalSeconds = 1,
                MinimumGapSeconds = 0,
                MaxWidthPixels = 99_999,
                Quality = 0,
                KeepForDays = 0,
            },
        };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Equal(SettingLimits.DayStartHour.Max, normalized.General.DayStartHour);
        Assert.Equal(SettingLimits.AwayAfterMinutes.Min, normalized.Tracking.AwayAfterMinutes);
        Assert.Equal(SettingLimits.AwayPromptAfterMinutes.Min, normalized.Tracking.AwayPromptAfterMinutes);
        Assert.Equal(SettingLimits.ScreenshotIntervalSeconds.Min, normalized.Screenshots.IntervalSeconds);
        Assert.Equal(SettingLimits.ScreenshotMinimumGapSeconds.Min, normalized.Screenshots.MinimumGapSeconds);
        Assert.Equal(SettingLimits.ScreenshotMaxWidthPixels.Max, normalized.Screenshots.MaxWidthPixels);
        Assert.Equal(SettingLimits.ScreenshotQuality.Min, normalized.Screenshots.Quality);
        Assert.Equal(SettingLimits.KeepScreenshotsForDays.Min, normalized.Screenshots.KeepForDays);
    }

    [Fact]
    public void NormalizedReplacesUnknownChoicesWithDefaults()
    {
        // Arrange
        var settings = new AppSettings
        {
            Screenshots = new ScreenshotSettings { PreviewMode = (ScreenshotPreviewMode)99 },
        };

        // Act
        var normalized = settings.Normalized();

        // Assert
        Assert.Equal(new ScreenshotSettings().PreviewMode, normalized.Screenshots.PreviewMode);
    }
}
