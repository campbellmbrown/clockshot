using System.Globalization;

namespace ClockShot.Core.Models;

/// <summary>
/// Gives each name a stable colour, so an application looks the same every day and on every machine.
/// </summary>
public static class NameColor
{
    // Middle values, so no colour is too pale or too dark.
    private const double Saturation = 0.55;
    private const double Lightness = 0.55;

    // The 32-bit FNV-1a hash's published starting value and multiplier.
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    private const int DegreesInCircle = 360;

    // The HSL formula splits the colour wheel into six equal sections.
    private const int DegreesPerSection = DegreesInCircle / 6;

    /// <summary>
    /// A "#RRGGBB" colour picked from the name. The same name, ignoring case, always gets the same colour.
    /// </summary>
    public static string For(string name)
    {
        var hue = Hash(name) % DegreesInCircle;
        var (r, g, b) = FromHsl(hue, Saturation, Lightness);
        return string.Create(CultureInfo.InvariantCulture, $"#{r:X2}{g:X2}{b:X2}");
    }

    // FNV-1a, because string.GetHashCode gives a different value each time the app runs.
    private static uint Hash(string name)
    {
        var hash = FnvOffsetBasis;
        foreach (var c in name.ToUpperInvariant())
        {
            hash ^= c;
            hash *= FnvPrime;
        }

        return hash;
    }

    /// <summary>
    /// The standard conversion from hue, saturation and lightness to red, green and blue.
    /// The names chroma, x and m follow the usual description of the formula.
    /// </summary>
    private static (byte R, byte G, byte B) FromHsl(double hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var x = chroma * (1 - Math.Abs(hue / DegreesPerSection % 2 - 1));
        var m = lightness - chroma / 2;
        var (r, g, b) = (hue / DegreesPerSection) switch
        {
            < 1 => (chroma, x, 0.0),
            < 2 => (x, chroma, 0.0),
            < 3 => (0.0, chroma, x),
            < 4 => (0.0, x, chroma),
            < 5 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x),
        };
        return (ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    private static byte ToByte(double value) => (byte)Math.Round(value * byte.MaxValue);
}
