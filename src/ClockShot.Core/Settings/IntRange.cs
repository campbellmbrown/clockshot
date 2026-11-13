namespace ClockShot.Core.Settings;

/// <summary>
/// An inclusive range of allowed whole-number values.
/// </summary>
public sealed record IntRange(int Min, int Max)
{
    public int Clamp(int value) => Math.Clamp(value, Min, Max);
}
