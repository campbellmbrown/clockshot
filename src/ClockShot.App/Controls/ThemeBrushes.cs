using System.Collections.Concurrent;

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace ClockShot.App.Controls;

/// <summary>
/// Brushes from <c>Themes/Colors.axaml</c> for the current theme, for code that picks a colour itself.
/// </summary>
public static class ThemeBrushes
{
    private static readonly ConcurrentDictionary<(string Key, ThemeVariant Theme), IBrush> BrushesByKey = new();

    public static IBrush StateActive => Get("StateActive");
    public static IBrush StateAway => Get("StateAway");
    public static IBrush StateLocked => Get("StateLocked");
    public static IBrush StatePaused => Get("StatePaused");

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
