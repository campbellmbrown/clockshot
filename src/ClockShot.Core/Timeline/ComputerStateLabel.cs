using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// The name a computer state is shown with on the timeline and in the details panel.
/// </summary>
public static class ComputerStateLabel
{
    public static string Of(ComputerState state) => state switch
    {
        ComputerState.Active => "Active",
        ComputerState.Away => "Away",
        ComputerState.Locked => "Locked",
        _ => state.ToString(),
    };
}
