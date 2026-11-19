namespace ClockShot.Core.Models;

/// <summary>
/// Who the work is for.
/// Projects optionally belong to a client.
/// </summary>
public sealed record Client(long Id, string Name, bool IsArchived);
