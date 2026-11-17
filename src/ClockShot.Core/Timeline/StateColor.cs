using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// The theme's colour for a computer state.
/// </summary>
public sealed record StateColor(ComputerState State) : IBlockColor;
