namespace ClockShot.Core.Models;

/// <summary>
/// A continuous stretch of time with the same foreground application and window title.
/// </summary>
public sealed record ActivitySegment(long Id, TimeRange Range, long ApplicationId, string WindowTitle);
