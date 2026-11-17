namespace ClockShot.Core.Timeline;

/// <summary>
/// How long one application was used.
/// </summary>
public sealed record ApplicationTotal(long ApplicationId, TimeSpan Duration);
