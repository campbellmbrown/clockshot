using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Logging;
using Avalonia.Media;

namespace ClockShot.App.Controls;

/// <summary>
/// Draws an icon in the current text colour.
/// The colour follows the theme, and dims when the icon's container is disabled.
/// </summary>
public sealed class Icon : Control
{
    /// <summary>
    /// The size in pixels when no size is set. It's the usual size for a menu icon.
    /// </summary>
    private const double DefaultSize = 16;

    public static readonly StyledProperty<string?> KindProperty =
        AvaloniaProperty.Register<Icon, string?>(nameof(Kind));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<Icon>();

    private IReadOnlyList<IconShape> _shapes = [];

    static Icon()
    {
        WidthProperty.OverrideDefaultValue<Icon>(DefaultSize);
        HeightProperty.OverrideDefaultValue<Icon>(DefaultSize);
        AffectsRender<Icon>(KindProperty, ForegroundProperty);
    }

    /// <summary>
    /// The icon name, one of the <see cref="Icons"/> constants.
    /// </summary>
    public string? Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KindProperty)
        {
            _shapes = Kind is { Length: > 0 } kind ? LoadShapes(kind) : [];
        }
    }

    /// <summary>
    /// An unknown name draws nothing instead of throwing.
    /// Menus create their icons when they open, so a throw would crash the app on a right-click.
    /// IconsTests catches unknown names before they ship.
    /// </summary>
    private IReadOnlyList<IconShape> LoadShapes(string kind)
    {
        if (IconGeometry.TryLoad(kind, out var shapes))
        {
            return shapes;
        }

        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "Icon '{Kind}' is not in Assets/Icons", kind);
        return shapes;
    }

    public override void Render(DrawingContext context)
    {
        if (_shapes.Count == 0 || Foreground is not { } brush)
        {
            return;
        }

        IconGeometry.Draw(context, _shapes, brush, new Rect(Bounds.Size));
    }
}
