using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ClockShot.App.Input;

/// <summary>
/// Text that mentions a shortcut, with the keys taken from <see cref="Shortcuts"/> so it can't go out of date:
/// <c>ToolTip.Tip="{input:WithKeys 'Next day ({0})', {x:Static input:Shortcuts.NextDay}}"</c> gives "Next day (Alt+Right)".
/// </summary>
public sealed class WithKeysExtension(string format, KeyGesture gesture) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, format, Shortcuts.Describe(gesture));
}
