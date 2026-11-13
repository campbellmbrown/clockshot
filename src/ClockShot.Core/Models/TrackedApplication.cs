namespace ClockShot.Core.Models;

/// <summary>
/// An application seen by the tracker, identified by its executable.
/// </summary>
public sealed record TrackedApplication(
    long Id,
    string ProcessName,
    string? ExecutablePath,
    string DisplayName,
    string ColorHex);
