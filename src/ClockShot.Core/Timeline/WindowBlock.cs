using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// What a block on the Applications lane stands for, which is one window of one application.
/// </summary>
public sealed record WindowBlock(TrackedApplication Application, string Title);
