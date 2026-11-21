using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ClockShot.App.Controls;

/// <summary>
/// Turns a stored "#RRGGBB" colour into the <see cref="Color"/> that the colour picker edits, and back again.
/// It works both ways, so the picker can be bound to a view model's colour text.
/// </summary>
public sealed class HexColorConverter : IValueConverter
{
    public static HexColorConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string hex && Color.TryParse(hex, out var color) ? color : Colors.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Color color ? string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}") : null;
}
