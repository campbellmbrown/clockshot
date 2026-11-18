namespace ClockShot.Core.Timeline;

/// <summary>
/// The lengths of time the zoom box steps through with its arrows or the mouse wheel.
/// </summary>
public static class ZoomSteps
{
    /// <summary>
    /// How far a length can be from a step and still count as that step.
    /// Zooming with the mouse can leave a length just off a step.
    /// Without this, the next step from there could be that same step, and the zoom box would seem to do nothing.
    /// </summary>
    private const double Tolerance = 0.01;

    /// <summary>
    /// Round lengths, from <see cref="TimelineViewport.MinVisibleDuration"/> to a whole day.
    /// </summary>
    private static readonly int[] StepMinutes = [1, 2, 5, 10, 15, 30, 60, 120, 180, 240, 360, 480, 720, 1440];

    private static readonly TimeSpan[] Steps = [.. StepMinutes.Select(minutes => TimeSpan.FromMinutes(minutes))];

    /// <summary>
    /// The next step longer than the current length, or null when there's none.
    /// </summary>
    public static TimeSpan? Longer(TimeSpan current)
    {
        foreach (var step in Steps)
        {
            if (step > current * (1 + Tolerance))
            {
                return step;
            }
        }

        return null;
    }

    /// <summary>
    /// The next step shorter than the current length, or null when there's none.
    /// </summary>
    public static TimeSpan? Shorter(TimeSpan current)
    {
        foreach (var step in Steps.Reverse())
        {
            if (step < current * (1 - Tolerance))
            {
                return step;
            }
        }

        return null;
    }
}
