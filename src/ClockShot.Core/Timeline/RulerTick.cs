namespace ClockShot.Core.Timeline;

/// <summary>
/// A tick on the time ruler.
/// Major ticks have a label, and minor ticks have none.
/// </summary>
public sealed record RulerTick(DateTime TimeUtc, string? Label);
