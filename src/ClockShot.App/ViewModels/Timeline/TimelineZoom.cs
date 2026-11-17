using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// How much of the day the timeline shows, and which part.
/// It turns zoom steps into changes to the <see cref="TimelineViewport"/>.
/// </summary>
public sealed partial class TimelineZoom : ViewModelBase
{
    /// <summary>
    /// Each zoom step divides or multiplies the visible time by this.
    /// </summary>
    private const double ZoomStep = 1.5;

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
    /// Zooms around a point on the drawing area.
    /// Positive steps zoom in.
    /// </summary>
    public void ZoomAt(double x, double steps) => Viewport = Viewport.ZoomAt(x, Math.Pow(ZoomStep, steps));

    /// <summary>
    /// Shows the whole range, keeping the current width.
    /// </summary>
    public void Fit(TimeRange range) => Viewport = TimelineViewport.Fit(range, Viewport.Width);

    public void CenterOn(DateTime timeUtc) => Viewport = Viewport.CenterOn(timeUtc);

    partial void OnViewportChanged(TimelineViewport value) => ViewportChanged?.Invoke();
}
