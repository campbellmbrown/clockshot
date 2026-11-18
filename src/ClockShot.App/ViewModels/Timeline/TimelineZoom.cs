using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// How much of the day the timeline shows, and which part.
/// It turns zoom steps, pans and the zoom box into changes to the <see cref="TimelineViewport"/>.
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
    [NotifyPropertyChangedFor(nameof(VisibleDurationText))]
    public partial TimelineViewport Viewport { get; private set; }

    /// <summary>
    /// How much time is visible, as the zoom box shows it.
    /// Typing a length into the box zooms to it.
    /// </summary>
    public string VisibleDurationText => DurationText.Format(Viewport.Visible.Duration);

    /// <summary>
    /// Shows the next longer length in <see cref="ZoomSteps"/>.
    /// </summary>
    public void ShowLongerDuration()
    {
        if (ZoomSteps.Longer(Viewport.Visible.Duration) is { } longer)
        {
            Viewport = Viewport.WithVisibleDuration(longer);
        }
    }

    /// <summary>
    /// Shows the next shorter length in <see cref="ZoomSteps"/>.
    /// </summary>
    public void ShowShorterDuration()
    {
        if (ZoomSteps.Shorter(Viewport.Visible.Duration) is { } shorter)
        {
            Viewport = Viewport.WithVisibleDuration(shorter);
        }
    }

    /// <summary>
    /// Called by the control when its drawing area is resized.
    /// </summary>
    public void SetWidth(double width) => Viewport = Viewport.WithWidth(width);

    /// <summary>
    /// Zooms around a point on the drawing area.
    /// Positive steps zoom in.
    /// </summary>
    public void ZoomAt(double x, double steps) => Viewport = Viewport.ZoomAt(x, Math.Pow(ZoomStep, steps));

    public void PanBy(double pixels) => Viewport = Viewport.PanBy(pixels);

    /// <summary>
    /// Shows the whole range, keeping the current width.
    /// </summary>
    public void Fit(TimeRange range) => Viewport = TimelineViewport.Fit(range, Viewport.Width);

    public void CenterOn(DateTime timeUtc) => Viewport = Viewport.CenterOn(timeUtc);

    /// <summary>
    /// Zooms to the length typed in the zoom box, keeping the middle in place.
    /// </summary>
    [RelayCommand]
    private void ShowDuration(string? text)
    {
        if (DurationText.TryParse(text, out var duration))
        {
            Viewport = Viewport.WithVisibleDuration(duration);
        }

        // Puts the length actually shown back in the box, written the usual way.
        // It also differs from what was typed when the text can't be read, or the zoom limits change the length.
        OnPropertyChanged(nameof(VisibleDurationText));
    }

    partial void OnViewportChanged(TimelineViewport value) => ViewportChanged?.Invoke();
}
