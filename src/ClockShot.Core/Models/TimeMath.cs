namespace ClockShot.Core.Models;

/// <summary>
/// Small time helpers that LINQ and <see cref="DateTime"/> don't provide.
/// </summary>
public static class TimeMath
{
    public static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}
