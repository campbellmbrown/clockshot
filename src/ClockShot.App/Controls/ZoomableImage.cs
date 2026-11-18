using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ClockShot.App.Controls;

/// <summary>
/// Shows an image scaled up or down to fit, keeping its shape.
/// The mouse wheel zooms in around the pointer, and dragging moves the zoomed image.
/// Double-clicking goes back to fit, and a new image starts fitted.
/// </summary>
public sealed class ZoomableImage : Control
{
    public const double MaxZoom = 8;
    private const double WheelZoomStep = 1.25;

    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<ZoomableImage, Bitmap?>(nameof(Source));

    public static readonly DirectProperty<ZoomableImage, double> ZoomProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, double>(nameof(Zoom), image => image.Zoom);

    // Shared, because each Cursor holds a native handle.
    private static readonly Cursor ArrowCursor = new(StandardCursorType.Arrow);
    private static readonly Cursor DragCursor = new(StandardCursorType.SizeAll);

    private double _zoom = 1;

    // How far the image's centre is from the control's centre, in pixels.
    private Vector _pan;

    private Point? _dragFrom;

    static ZoomableImage()
    {
        AffectsRender<ZoomableImage>(SourceProperty);
        AffectsMeasure<ZoomableImage>(SourceProperty);
        ClipToBoundsProperty.OverrideDefaultValue<ZoomableImage>(true);
    }

    public ZoomableImage()
    {
        Cursor = ArrowCursor;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    public Bitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// How much the image is enlarged from fitting the control, from 1 up to <see cref="MaxZoom"/>.
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        private set => SetAndRaise(ZoomProperty, ref _zoom, value);
    }

    /// <summary>
    /// Zooms by <paramref name="factor"/>, keeping the image point under <paramref name="point"/> in place.
    /// </summary>
    public void ZoomAt(Point point, double factor)
    {
        var newZoom = Math.Clamp(Zoom * factor, 1, MaxZoom);
        var center = new Vector(Bounds.Width / 2, Bounds.Height / 2);
        var fromCenter = (Vector)point - center;

        // The point stays put when its offset from the image centre scales with the zoom.
        _pan = fromCenter - (fromCenter - _pan) * (newZoom / Zoom);
        Zoom = newZoom;
        ClampPan();
        InvalidateVisual();
    }

    public void ResetZoom()
    {
        Zoom = 1;
        _pan = default;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (Source is not { } source)
        {
            return;
        }

        var rect = ImageRect(source);
        context.DrawImage(source, new Rect(source.Size), rect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty)
        {
            ResetZoom();
        }
    }

    /// <summary>
    /// Asks for the image's size, scaled up or down to fit the space available, keeping its shape.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Source is not { } source)
        {
            return default;
        }

        var scaleX = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / source.Size.Width;
        var scaleY = double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : availableSize.Height / source.Size.Height;
        var scale = Math.Min(scaleX, scaleY);
        return double.IsInfinity(scale) ? source.Size : source.Size * scale;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (Source is null)
        {
            return;
        }

        ZoomAt(e.GetPosition(this), Math.Pow(WheelZoomStep, e.Delta.Y));
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ResetZoom();
            return;
        }

        if (Zoom <= 1)
        {
            return;
        }

        _dragFrom = e.GetPosition(this);
        e.Pointer.Capture(this);
        Cursor = DragCursor;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragFrom is not { } from)
        {
            return;
        }

        var point = e.GetPosition(this);
        _pan += point - from;
        _dragFrom = point;
        ClampPan();
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        e.Pointer.Capture(null);
        EndDrag();
    }

    /// <summary>
    /// Capture can also be lost without a release, such as when another window takes the mouse mid-drag.
    /// </summary>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndDrag();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ClampPan();
    }

    /// <summary>
    /// Where the image is drawn, fitted and centred, then zoomed and moved.
    /// </summary>
    private Rect ImageRect(Bitmap source)
    {
        var size = ZoomedSize(source);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2) + _pan;
        return new Rect(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);
    }

    /// <summary>
    /// Keeps the zoomed image covering the view, so it can't be dragged off-screen.
    /// </summary>
    private void ClampPan()
    {
        if (Source is not { } source)
        {
            return;
        }

        var size = ZoomedSize(source);
        var maxX = Math.Max(0, (size.Width - Bounds.Width) / 2);
        var maxY = Math.Max(0, (size.Height - Bounds.Height) / 2);
        _pan = new Vector(Math.Clamp(_pan.X, -maxX, maxX), Math.Clamp(_pan.Y, -maxY, maxY));
    }

    /// <summary>
    /// The image's size when fitted to the control, times the zoom.
    /// </summary>
    private Size ZoomedSize(Bitmap source) =>
        source.Size * Math.Min(Bounds.Width / source.Size.Width, Bounds.Height / source.Size.Height) * Zoom;

    private void EndDrag()
    {
        _dragFrom = null;
        Cursor = ArrowCursor;
    }
}
