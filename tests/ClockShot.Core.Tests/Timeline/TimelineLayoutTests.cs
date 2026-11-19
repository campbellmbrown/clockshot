using ClockShot.Core.Settings;
using ClockShot.Core.Tests.Settings;
using ClockShot.Core.Timeline;

namespace ClockShot.Core.Tests.Timeline;

public sealed class TimelineLayoutTests
{
    private readonly FakeSettingsStore _settings = new();

    [Fact]
    public void ReadsTheSavedSizes()
    {
        // Arrange
        var saved = new LayoutSettings { TimelineHeight = 250, DetailsPanelWidth = 400 };
        _settings.Save(new AppSettings { Layout = saved });
        var layout = new TimelineLayout(_settings);

        // Act
        var height = layout.TimelineHeight;
        var width = layout.DetailsPanelWidth;

        // Assert
        Assert.Equal(250, height);
        Assert.Equal(400, width);
    }

    [Fact]
    public void SavesSizesInWholePixels()
    {
        // Arrange
        var layout = new TimelineLayout(_settings);

        // Act
        layout.Save(timelineHeight: 300.4, detailsPanelWidth: 359.6);

        // Assert
        Assert.Equal(new LayoutSettings { TimelineHeight = 300, DetailsPanelWidth = 360 }, _settings.Current.Layout);
    }

    [Fact]
    public void DoesNotSaveSizesThatHaveNotChanged()
    {
        // Arrange
        var layout = new TimelineLayout(_settings);
        var saves = 0;
        _settings.Changed += _ => saves++;

        // Act
        layout.Save(
            timelineHeight: layout.TimelineHeight + 0.2,
            detailsPanelWidth: layout.DetailsPanelWidth - 0.2);

        // Assert
        Assert.Equal(0, saves);
    }
}
