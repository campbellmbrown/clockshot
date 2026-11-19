using ClockShot.Core.Settings;

namespace ClockShot.Core.Timeline;

/// <summary>
/// The Timeline tab's divider positions, as the user last dragged them.
/// </summary>
public sealed class TimelineLayout(ISettingsStore settings)
{
    public double TimelineHeight => settings.Current.Layout.TimelineHeight;

    public double DetailsPanelWidth => settings.Current.Layout.DetailsPanelWidth;

    /// <summary>
    /// Remembers the sizes after the user drags a divider.
    /// They're saved in whole pixels, so a drag of less than a pixel doesn't rewrite the settings file.
    /// </summary>
    public void Save(double timelineHeight, double detailsPanelWidth)
    {
        var current = settings.Current;
        var layout = current.Layout with
        {
            TimelineHeight = Math.Round(timelineHeight),
            DetailsPanelWidth = Math.Round(detailsPanelWidth),
        };
        if (layout != current.Layout)
        {
            settings.Save(current with { Layout = layout });
        }
    }
}
