using ClockShot.App.Controls;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// The Timeline tab, which shows one day of recorded activity.
/// </summary>
public sealed partial class TimelineViewModel : PageViewModel
{
    public TimelineViewModel()
        : base("Timeline", Icons.Timeline)
    {
    }
}
