namespace ClockShot.Core;

/// <summary>
/// Compares doubles while allowing for rounding errors.
/// The tolerance suits values the size of screen pixels, not large values such as times in seconds.
/// </summary>
public static class FloatingPoint
{
    // Rounding errors in values the size of screen pixels are far smaller than this.
    private const double Tolerance = 1e-9;

    public static bool AreEqual(double a, double b) => Math.Abs(a - b) <= Tolerance;
}
