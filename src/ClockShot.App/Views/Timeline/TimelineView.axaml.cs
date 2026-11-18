using Avalonia.Controls;
using Avalonia.Input;

namespace ClockShot.App.Views.Timeline;

/// <summary>
/// The Timeline tab's behaviour that only the view needs.
/// It keeps the preview popup beside the pointer.
/// </summary>
public sealed partial class TimelineView : UserControl
{
    /// <summary>
    /// The popup preview can use up to this share of the window's width and height.
    /// </summary>
    private const double PopupShareOfWindow = 0.6;
    private const double PopupPointerGap = 16;

    public TimelineView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Keeps the popup beside the pointer, without running off the right edge of the window.
    /// </summary>
    private void OnTimelinePointerMoved(object? sender, PointerEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not { } window)
        {
            return;
        }

        PopupPreview.MaxWidth = window.Bounds.Width * PopupShareOfWindow;
        PopupPreview.MaxHeight = window.Bounds.Height * PopupShareOfWindow;

        var x = e.GetPosition(TimelineArea).X;

        // Before its first layout the popup has no width, so it's taken to be as wide as the preview can grow.
        var laidOutWidth = PreviewPopup.Child?.Bounds.Width ?? 0;
        var popupWidth = laidOutWidth > 0 ? laidOutWidth : PopupPreview.MaxWidth;
        var rightmostOffset = Math.Max(0, TimelineArea.Bounds.Width - popupWidth);
        PreviewPopup.HorizontalOffset = Math.Clamp(x + PopupPointerGap, min: 0, max: rightmostOffset);
    }
}
