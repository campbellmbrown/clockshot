using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// How much of the day the timeline shows, and which part.
/// </summary>
public sealed partial class TimelineZoom : ViewModelBase
{
    /// <summary>
    /// Raised when the viewport changes, so the timeline redraws.
    /// </summary>
    public event Action? ViewportChanged;

    [ObservableProperty]
    public partial TimelineViewport Viewport { get; private set; }

    /// <summary>
    /// Called by the control when its drawing area is resized.
    /// </summary>
    public void SetWidth(double width) => Viewport = Viewport.WithWidth(width);

    /// <summary>
    /// Shows the whole range, keeping the current width.
    /// </summary>
    public void Fit(TimeRange range) => Viewport = TimelineViewport.Fit(range, Viewport.Width);

    partial void OnViewportChanged(TimelineViewport value) => ViewportChanged?.Invoke();
}
