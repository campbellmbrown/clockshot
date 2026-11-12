using Avalonia;
using Avalonia.Controls;

namespace ClockShot.App.Controls;

/// <summary>
/// Sets a menu item's icon by name.
/// </summary>
public static class MenuIcon
{
    public static readonly AttachedProperty<string?> KindProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, string?>("Kind", typeof(MenuIcon));

    static MenuIcon() =>
        KindProperty.Changed.AddClassHandler<MenuItem>((item, e) =>
        {
            if (item.Icon is Icon icon)
            {
                icon.Kind = e.GetNewValue<string?>();
            }
            else
            {
                item.Icon = new Icon { Kind = e.GetNewValue<string?>() };
            }
        });

    public static string? GetKind(MenuItem item) => item.GetValue(KindProperty);

    public static void SetKind(MenuItem item, string? value) => item.SetValue(KindProperty, value);
}
