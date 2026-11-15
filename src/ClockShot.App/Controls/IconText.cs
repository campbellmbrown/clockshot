using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ClockShot.App.Controls;

/// <summary>
/// An icon followed by text, the content of most buttons.
/// </summary>
public sealed class IconText : StackPanel
{
    private const double GapBetweenIconAndText = 6;

    public static readonly StyledProperty<string?> KindProperty = AvaloniaProperty.Register<IconText, string?>(nameof(Kind));

    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<IconText, string?>(nameof(Text));

    public IconText()
    {
        Orientation = Orientation.Horizontal;
        Spacing = GapBetweenIconAndText;

        var icon = new Icon();
        icon.Bind(Icon.KindProperty, this.GetObservable(KindProperty));
        var text = new TextBlock();
        text.Bind(TextBlock.TextProperty, this.GetObservable(TextProperty));

        Children.Add(icon);
        Children.Add(text);
    }

    public string? Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
