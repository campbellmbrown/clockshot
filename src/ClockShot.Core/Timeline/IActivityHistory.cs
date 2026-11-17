using ClockShot.Core.Models;

namespace ClockShot.Core.Timeline;

/// <summary>
/// Reads recorded activity back.
/// </summary>
public interface IActivityHistory
{
    /// <summary>
    /// The segments that overlap <paramref name="range"/>, sorted by start.
    /// </summary>
    Task<ActivityRecords> LoadAsync(TimeRange range, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every application seen so far.
    /// </summary>
    Task<IReadOnlyList<TrackedApplication>> GetApplicationsAsync();
}
