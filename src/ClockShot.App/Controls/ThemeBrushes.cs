using System.Collections.Concurrent;

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace ClockShot.App.Controls;

/// <summary>
/// Brushes from <c>Themes/Colors.axaml</c> for the current theme, for code that picks a colour itself.
/// It also makes solid brushes for stored colours such as an application's "#RRGGBB".
/// </summary>
public static class ThemeBrushes
{
    /// <summary>
    /// Shows a stored "#RRGGBB" colour in XAML.
    /// A missing or invalid colour shows as grey.
    /// </summary>
    public static readonly IValueConverter FromHex =
        new FuncValueConverter<string?, IBrush>(hex => ForColor(hex ?? ""));

    private static readonly ConcurrentDictionary<(string Key, ThemeVariant Theme), IBrush> BrushesByKey = new();
    private static readonly ConcurrentDictionary<string, IBrush> ColorBrushes = new(StringComparer.OrdinalIgnoreCase);

    public static IBrush StateActive => Get("StateActive");
    public static IBrush StateAway => Get("StateAway");
    public static IBrush StateLocked => Get("StateLocked");
    public static IBrush StatePaused => Get("StatePaused");

    /// <summary>
    /// A brush for a stored colour like "#3E8E5E".
    /// Invalid colours fall back to grey.
    /// </summary>
    public static IBrush ForColor(string hex) =>
        ColorBrushes.GetOrAdd(
            hex,
            h => new ImmutableSolidColorBrush(Color.TryParse(h, out var color) ? color : Colors.Gray));

    private static IBrush Get(string key)
    {
        var app = Application.Current;
        var theme = app?.ActualThemeVariant ?? ThemeVariant.Dark;
        // A missing key shows as magenta, so it's obvious on screen.
        return BrushesByKey.GetOrAdd((key, theme), _ =>
            app is not null && app.TryGetResource(key, theme, out var value) && value is IBrush brush
                ? brush.ToImmutable()
                : Brushes.Magenta);
    }
}
