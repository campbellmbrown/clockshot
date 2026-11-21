using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// What a block on the Tags lane stands for, which is one tagged time entry.
/// </summary>
public sealed record TagBlock(TimeEntry Entry);
