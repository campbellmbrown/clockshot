using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using ClockShot.App.ViewModels.Timeline;

namespace ClockShot.App.Views.Timeline;

/// <summary>
/// The Timeline tab's behaviour that only the view needs.
/// It runs the zoom spinner.
/// </summary>
public sealed partial class TimelineView : UserControl
{
    private TimelineViewModel? _timeline;

    public TimelineView()
    {
        InitializeComponent();

        // The spinner reacts to the wheel only when it has keyboard focus, so this handler reacts whenever the pointer
        // is over it.
        VisibleDurationSpinner.AddHandler(PointerWheelChangedEvent, OnVisibleDurationWheel, RoutingStrategies.Tunnel);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _timeline = DataContext as TimelineViewModel;
    }

    /// <summary>
    /// The spinner's up arrow shows more time, and its down arrow shows less.
    /// </summary>
    private void OnVisibleDurationSpin(object? sender, SpinEventArgs e)
    {
        if (e.Direction == SpinDirection.Increase)
        {
            _timeline?.Zoom.ShowLongerDuration();
        }
        else
        {
            _timeline?.Zoom.ShowShorterDuration();
        }
    }

    /// <summary>
    /// Scrolling up over the spinner shows more time, and scrolling down shows less, like the arrows.
    /// </summary>
    private void OnVisibleDurationWheel(object? sender, PointerWheelEventArgs e)
    {
        if (_timeline is null || e.Delta.Y == 0)
        {
            return;
        }

        if (e.Delta.Y > 0)
        {
            _timeline.Zoom.ShowLongerDuration();
        }
        else
        {
            _timeline.Zoom.ShowShorterDuration();
        }

        e.Handled = true;
    }

    /// <summary>
    /// Leaving the zoom box applies what was typed, like pressing Enter.
    /// </summary>
    private void OnVisibleDurationLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_timeline is not null && VisibleDurationBox.Text != _timeline.Zoom.VisibleDurationText)
        {
            _timeline.Zoom.ShowDurationCommand.Execute(VisibleDurationBox.Text);
        }
    }
}
