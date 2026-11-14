using Avalonia.Data.Converters;
using Avalonia.Media;

using ClockShot.App.ViewModels;

namespace ClockShot.App.Controls;

/// <summary>
/// Turns the status bar's state into the colour of its dot.
/// </summary>
public static class StatusBarBrushes
{
    public static readonly IValueConverter FromState = new FuncValueConverter<StatusBarState, IBrush>(For);

    public static IBrush For(StatusBarState state) => state switch
    {
        StatusBarState.Tracking => ThemeBrushes.StateActive,
        StatusBarState.Paused => ThemeBrushes.StatePaused,
        StatusBarState.Away => ThemeBrushes.StateAway,
        // Before the first sample there's no state to colour, so it shows the same neutral grey as Locked.
        StatusBarState.Locked or StatusBarState.Starting => ThemeBrushes.StateLocked,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown status bar state."),
    };
}
