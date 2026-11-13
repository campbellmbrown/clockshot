namespace ClockShot.Core.Models;

/// <summary>
/// A continuous stretch of time in one computer state.
/// </summary>
public sealed record ComputerStateSegment(long Id, TimeRange Range, ComputerState State);
