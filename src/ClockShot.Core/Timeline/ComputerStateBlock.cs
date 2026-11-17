using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// What a block on the Computer lane stands for.
/// </summary>
public sealed record ComputerStateBlock(ComputerState State);
