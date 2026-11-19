using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using ClockShot.App.Input;
using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core;
using ClockShot.Core.Timeline;

namespace ClockShot.App.Controls;

/// <summary>
/// Draws the timeline and turns pointer input into calls on <see cref="TimelineViewModel"/>, which owns the behaviour.
/// It draws the ruler, the lanes, the selection, the hover line and the now line.
/// Any number of lanes is drawn the same way.
/// </summary>
/// <remarks>
/// The mouse wheel zooms around the pointer.
/// Shift with the wheel, or dragging the ruler, scrolls.
/// Clicking a block selects it, and dragging across lanes selects a time range.
/// Dragging an edge of the selection moves that edge.
/// Drags snap to block edges unless Alt is held.
/// See <see cref="SelectionDrag"/>.
/// </remarks>
public sealed class TimelineControl : Control
{
    public const double LaneTitleWidth = 110;
    public const double RulerHeight = 28;

    /// <summary>
    /// Rows stretch to fill the control's height, but never get shorter than this.
    /// </summary>
    public const double MinLaneHeight = 22;

    public static readonly StyledProperty<TimelineViewModel?> TimelineProperty =
        AvaloniaProperty.Register<TimelineControl, TimelineViewModel?>(nameof(Timeline));

    // The space above and below each block, within its row.
    private const double BlockPadding = 3;

    private const double MinLabelWidth = 28;

    // How far the pointer must move while pressed, in pixels, before a click becomes a drag.
    private const double DragThreshold = 3;

    // One wheel notch scrolls about as far as it scrolls three lines of text in most apps.
    private const double WheelPanPixels = 60;

    // Text sizes are in pixels.
    // Ruler labels are smaller than lane titles and block labels.
    private const double RulerTextSize = 11;
    private const double LabelTextSize = 12;

    // The lane title starts this far in, and stops this far before the drawing area.
    private const double LaneTitleLeft = 10;
    private const double LaneTitleRightGap = 6;

    private const double MajorTickHeight = 10;
    private const double MinorTickHeight = 4;

    // How far ruler labels sit from their tick, and how far they and the badges sit from the top of the ruler.
    private const double RulerLabelInset = 4;

    // How far a block's label sits inside the block's edges.
    private const double BlockLabelInset = 5;

    // The now line and the selection's edges are thicker than the grid and hover lines, so they stand out.
    private const double EmphasisPenThickness = 1.5;

    private const double SelectedBlockPenThickness = 2;

    // The now marker is a small triangle on top of the now line.
    private const double NowMarkerHalfWidth = 5;
    private const double NowMarkerHeight = 7;

    private const double PausedBarWidth = 3;

    // Badges are the small labels on the ruler for the hover time and a paused preview.
    private const double BadgeHorizontalPadding = 4;
    private const double PausedBadgeHorizontalPadding = 5;
    private const double BadgeVerticalPadding = 1;

    // A float, because that's what Avalonia's FillRectangle takes for a corner radius.
    private const float BadgeCornerRadius = 3;

    // A block or selection narrower than a pixel is still drawn one pixel wide, so it doesn't vanish.
    private const double MinDrawnWidth = 1;

    // This is many times the number of labels in one day.
    // Past it the cache is cleared, so it can't grow without limit.
    private const int MaxCachedTexts = 2000;

    // Before its first layout the control is no wider than the lane titles, which would leave no drawing area.
    private const double MinAreaWidth = 1;

    private static readonly Typeface LabelFont = new(FontFamily.Default);
    private static readonly Cursor ArrowCursor = new(StandardCursorType.Arrow);
    private static readonly Cursor ResizeCursor = new(StandardCursorType.SizeWestEast);

    private readonly Dictionary<(string Text, double Size), FormattedText> _textCache = [];
    private Pens? _pens;
    private StreamGeometry? _nowMarker;
    private Gesture _gesture;
    private Point _pressPoint;
    private int _pressLane;
    private int _measuredLaneCount;

    public TimelineControl()
    {
        ClipToBounds = true;
        Focusable = true;
        Cursor = ArrowCursor;
        ActualThemeVariantChanged += (_, _) =>
        {
            // The pens are made again from the new theme's brushes on the next draw.
            _pens = null;
            InvalidateVisual();
        };
    }

    private enum Gesture
    {
        None,

        /// <summary>
        /// Pressed on a lane.
        /// It's a click unless the pointer moves, which starts a new selection as a <see cref="Drag"/>.
        /// </summary>
        PendingClick,

        /// <summary>
        /// Pressed on the ruler, so dragging scrolls.
        /// </summary>
        Pan,

        /// <summary>
        /// Moving one end of the selection, either a new one or an edge that was grabbed.
        /// </summary>
        Drag,
    }

    public TimelineViewModel? Timeline
    {
        get => GetValue(TimelineProperty);
        set => SetValue(TimelineProperty, value);
    }

    /// <summary>
    /// The height of each row, which shares the space below the ruler equally between the rows.
    /// </summary>
    public double LaneHeight => Math.Max(MinLaneHeight, (Bounds.Height - RulerHeight) / LaneCount);

    /// <summary>
    /// The x of the drawing area's left edge, after the lane titles.
    /// </summary>
    private static double AreaLeft => LaneTitleWidth;

    private double AreaWidth => Math.Max(MinAreaWidth, Bounds.Width - AreaLeft);

    /// <summary>
    /// The part of the control right of the lane titles, where times are drawn.
    /// </summary>
    private Rect DrawingArea => new(AreaLeft, 0, AreaWidth, Bounds.Height);

    private int LaneCount => Math.Max(1, Timeline?.Lanes.Count ?? 0);

    private Pens ThemePens => _pens ??= Pens.ForCurrentTheme();

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(ThemeBrushes.Background, new Rect(Bounds.Size));
        if (Timeline is not { } timeline)
        {
            return;
        }

        var viewport = timeline.Zoom.Viewport;

        DrawLaneBackgrounds(context, timeline);
        using (context.PushClip(DrawingArea))
        {
            DrawRuler(context, viewport, timeline.TimeZone);
            for (var i = 0; i < timeline.Lanes.Count; i++)
            {
                DrawBlocks(context, viewport, timeline, timeline.Lanes[i], LaneTop(i));
            }

            DrawSelection(context, viewport, timeline);
            DrawPreviewPause(context, viewport, timeline);
            DrawNowLine(context, viewport, timeline.NowUtc);
            DrawSnapPreview(context, viewport, timeline);
            DrawHoverLine(context, viewport, timeline);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TimelineProperty)
        {
            if (change.OldValue is TimelineViewModel old)
            {
                old.RedrawNeeded -= OnRedrawNeeded;
            }

            if (change.NewValue is TimelineViewModel timeline)
            {
                timeline.RedrawNeeded += OnRedrawNeeded;
                ReportWidth(timeline);
            }

            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (change.Property == BoundsProperty && Timeline is { } timeline)
        {
            ReportWidth(timeline);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _measuredLaneCount = LaneCount;
        return new Size(0, RulerHeight + MinLaneHeight * LaneCount);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Timeline is not { } timeline)
        {
            return;
        }

        var point = e.GetPosition(this);
        if (point.X < AreaLeft)
        {
            return;
        }

        var properties = e.GetCurrentPoint(this).Properties;

        if (properties.IsRightButtonPressed)
        {
            // A right-click on a block outside the selection selects it first, so the context menu acts on it.
            if (timeline.Selection.Range?.Contains(TimeAt(timeline, point)) != true)
            {
                timeline.ClickAt(TimeAt(timeline, point), KindOf(timeline, LaneAt(point.Y)));
            }

            return;
        }

        if (!properties.IsLeftButtonPressed)
        {
            return;
        }

        Focus();
        e.Pointer.Capture(this);
        e.Handled = true;
        _pressPoint = point;
        _pressLane = LaneAt(point.Y);
        if (_pressLane < 0)
        {
            _gesture = Gesture.Pan;
        }
        else if (timeline.TryGrabEdge(TimeAt(timeline, point)))
        {
            _gesture = Gesture.Drag;
        }
        else
        {
            _gesture = Gesture.PendingClick;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (Timeline is not { } timeline)
        {
            return;
        }

        var point = e.GetPosition(this);

        switch (_gesture)
        {
            case Gesture.Pan:
                timeline.Zoom.PanBy(_pressPoint.X - point.X);
                _pressPoint = point;
                break;
            case Gesture.PendingClick when Math.Abs(point.X - _pressPoint.X) > DragThreshold:
                timeline.StartSelecting(TimeAt(timeline, _pressPoint), snap: Snaps(e.KeyModifiers));
                _gesture = Gesture.Drag;
                timeline.DragTo(TimeAt(timeline, point), snap: Snaps(e.KeyModifiers));
                break;
            case Gesture.Drag:
                timeline.DragTo(TimeAt(timeline, point), snap: Snaps(e.KeyModifiers));
                break;
            case Gesture.None:
                var isOverEdge = timeline.IsOverEdge(TimeAt(timeline, point));
                Cursor = isOverEdge ? ResizeCursor : ArrowCursor;
                break;
        }

        UpdateHover(timeline, point, e.KeyModifiers);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var gesture = _gesture;

        // Cleared first, so losing the capture below doesn't end the drag a second time.
        _gesture = Gesture.None;
        e.Pointer.Capture(null);
        if (Timeline is not { } timeline)
        {
            return;
        }

        if (gesture == Gesture.PendingClick)
        {
            timeline.ClickAt(TimeAt(timeline, _pressPoint), KindOf(timeline, _pressLane));
        }

        if (gesture == Gesture.Drag)
        {
            timeline.EndDrag();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        // Another window took the mouse mid-drag, so keep what was dragged so far.
        if (_gesture == Gesture.Drag)
        {
            Timeline?.EndDrag();
        }

        _gesture = Gesture.None;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Timeline?.EndHover();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Switching tabs removes the control without a pointer exit, which would leave the hover refreshing while hidden.
        Timeline?.EndHover();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (Timeline is not { } timeline)
        {
            return;
        }

        e.Handled = true;

        var point = e.GetPosition(this);
        if (Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y))
        {
            timeline.Zoom.PanBy(-e.Delta.X * WheelPanPixels);
        }
        else if (e.KeyModifiers.HasFlag(Shortcuts.ScrollModifier))
        {
            timeline.Zoom.PanBy(-e.Delta.Y * WheelPanPixels);
        }
        else
        {
            timeline.Zoom.ZoomAt(point.X - AreaLeft, e.Delta.Y);
        }

        // The time under the pointer just changed, so a drag's moving end follows it straight away.
        if (_gesture == Gesture.Drag)
        {
            timeline.DragTo(TimeAt(timeline, point), snap: Snaps(e.KeyModifiers));
        }
    }

    /// <summary>
    /// The small triangle on top of the now line.
    /// It points down and is centred on x = 0.
    /// </summary>
    private static StreamGeometry CreateNowMarker()
    {
        var marker = new StreamGeometry();
        using var geometry = marker.Open();
        geometry.BeginFigure(new Point(-NowMarkerHalfWidth, 0), isFilled: true);
        geometry.LineTo(new Point(NowMarkerHalfWidth, 0));
        geometry.LineTo(new Point(0, NowMarkerHeight));
        geometry.EndFigure(isClosed: true);
        return marker;
    }

    /// <summary>
    /// The x of a time on the control, centred on a pixel so a one-pixel line is drawn sharp.
    /// </summary>
    private static double LineX(TimelineViewport viewport, DateTime time) =>
        Math.Round(AreaLeft + viewport.XOf(time)) + 0.5;

    private static LaneKind? KindOf(TimelineViewModel timeline, int lane) =>
        lane >= 0 && lane < timeline.Lanes.Count ? timeline.Lanes[lane].Kind : null;

    private static DateTime TimeAt(TimelineViewModel timeline, Point point) =>
        timeline.Zoom.Viewport.TimeAt(point.X - AreaLeft);

    private static bool Snaps(KeyModifiers modifiers) => !modifiers.HasFlag(Shortcuts.NoSnapModifier);

    /// <summary>
    /// Tells the view model how wide the drawing area is, once the control has been laid out.
    /// Before that the width is 0, e.g. in a new control after switching back to this tab.
    /// Passing it on would squeeze the zoom.
    /// </summary>
    private void ReportWidth(TimelineViewModel timeline)
    {
        if (Bounds.Width > AreaLeft)
        {
            timeline.Zoom.SetWidth(AreaWidth);
        }
    }

    /// <summary>
    /// The control's height depends on the number of lanes, so it's measured again only when that changes.
    /// </summary>
    private void OnRedrawNeeded()
    {
        if (LaneCount != _measuredLaneCount)
        {
            InvalidateMeasure();
        }

        InvalidateVisual();
    }

    private void DrawLaneBackgrounds(DrawingContext context, TimelineViewModel timeline)
    {
        context.FillRectangle(ThemeBrushes.RulerBackground, new Rect(0, 0, Bounds.Width, RulerHeight));
        for (var i = 0; i < timeline.Lanes.Count; i++)
        {
            var top = LaneTop(i);
            var background = i % 2 == 0 ? ThemeBrushes.LaneBackground : ThemeBrushes.LaneAlternateBackground;
            context.FillRectangle(background, new Rect(0, top, Bounds.Width, LaneHeight));
            var titleWidth = LaneTitleWidth - LaneTitleLeft - LaneTitleRightGap;
            var title = Text(timeline.Lanes[i].Title, LabelTextSize, ThemeBrushes.SecondaryText, titleWidth);
            context.DrawText(title, new Point(LaneTitleLeft, top + (LaneHeight - title.Height) / 2));
        }

        // The divider sits on the middle of the lane titles' last pixel, so it's drawn sharp.
        context.DrawLine(ThemePens.Grid, new Point(AreaLeft - 0.5, 0), new Point(AreaLeft - 0.5, Bounds.Height));
    }

    private void DrawRuler(DrawingContext context, TimelineViewport viewport, TimeZoneInfo zone)
    {
        var tickPen = ThemePens.Tick;
        var gridPen = ThemePens.Grid;
        foreach (var tick in TimelineRuler.Ticks(viewport, zone))
        {
            var x = LineX(viewport, tick.TimeUtc);
            if (tick.Label is { } tickLabel)
            {
                context.DrawLine(gridPen, new Point(x, RulerHeight), new Point(x, Bounds.Height));
                context.DrawLine(tickPen, new Point(x, RulerHeight - MajorTickHeight), new Point(x, RulerHeight));
                var label = Text(tickLabel, RulerTextSize, ThemeBrushes.Tick);
                context.DrawText(label, new Point(x + RulerLabelInset, RulerLabelInset));
            }
            else
            {
                context.DrawLine(tickPen, new Point(x, RulerHeight - MinorTickHeight), new Point(x, RulerHeight));
            }
        }
    }

    /// <summary>
    /// Draws the lane's visible blocks.
    /// Only the first block in each pixel column is drawn, which keeps drawing fast when zoomed out.
    /// </summary>
    private void DrawBlocks(
        DrawingContext context,
        TimelineViewport viewport,
        TimelineViewModel timeline,
        TimelineLane lane,
        double top)
    {
        var lastPixel = double.NegativeInfinity;
        foreach (var block in lane.BlocksIn(viewport.Visible))
        {
            var left = AreaLeft + viewport.XOf(block.Range.Start);
            var right = AreaLeft + viewport.XOf(block.Range.End);
            var pixel = Math.Floor(left);
            if (right - left < MinDrawnWidth && pixel <= lastPixel)
            {
                continue;
            }

            lastPixel = Math.Floor(right);

            // Square and touching, so a row reads as one continuous bar at any zoom.
            var width = Math.Max(MinDrawnWidth, right - left);
            var rect = new Rect(left, top + BlockPadding, width, LaneHeight - BlockPadding * 2);
            context.FillRectangle(ThemeBrushes.For(block.Color), rect);
            if (timeline.Selection.IsSelected(lane, block))
            {
                context.DrawRectangle(null, ThemePens.SelectedBlock, rect.Deflate(SelectedBlockPenThickness / 2));
            }

            var labelLeft = Math.Max(left, AreaLeft) + BlockLabelInset;
            var labelWidth = Math.Min(right, AreaLeft + AreaWidth) - BlockLabelInset - labelLeft;
            if (labelWidth < MinLabelWidth)
            {
                continue;
            }

            var label = Text(block.Label, LabelTextSize, ThemeBrushes.Text, labelWidth);
            context.DrawText(label, new Point(labelLeft, rect.Y + (rect.Height - label.Height) / 2));
        }
    }

    private void DrawSelection(DrawingContext context, TimelineViewport viewport, TimelineViewModel timeline)
    {
        if (timeline.Selection.Range is not { } selection)
        {
            return;
        }

        var left = AreaLeft + viewport.XOf(selection.Start);
        var right = AreaLeft + viewport.XOf(selection.End);
        var rect = new Rect(left, RulerHeight, Math.Max(MinDrawnWidth, right - left), Bounds.Height - RulerHeight);
        context.FillRectangle(ThemeBrushes.SelectionFill, rect);
        var pen = ThemePens.Selection;
        context.DrawLine(pen, rect.TopLeft, rect.BottomLeft);
        context.DrawLine(pen, rect.TopRight, rect.BottomRight);
    }

    private void DrawNowLine(DrawingContext context, TimelineViewport viewport, DateTime nowUtc)
    {
        if (!viewport.Visible.Contains(nowUtc))
        {
            return;
        }

        var x = LineX(viewport, nowUtc);
        context.DrawLine(ThemePens.Now, new Point(x, 0), new Point(x, Bounds.Height));
        using (context.PushTransform(Matrix.CreateTranslation(x, 0)))
        {
            context.DrawGeometry(ThemeBrushes.NowLine, pen: null, _nowMarker ??= CreateNowMarker());
        }
    }

    /// <summary>
    /// Draws a coloured bar where the preview is paused, with "Paused" on the ruler.
    /// </summary>
    private void DrawPreviewPause(DrawingContext context, TimelineViewport viewport, TimelineViewModel timeline)
    {
        if (timeline.Preview.PausedAtUtc is not { } paused || !viewport.Visible.Contains(paused))
        {
            return;
        }

        var x = Math.Round(AreaLeft + viewport.XOf(paused));
        var brush = ThemeBrushes.PreviewPaused;

        // The bar is centred on x, and starts on a whole pixel so it's drawn sharp.
        var barLeft = x - Math.Floor(PausedBarWidth / 2);
        var bar = new Rect(barLeft, RulerHeight, PausedBarWidth, Bounds.Height - RulerHeight);
        context.FillRectangle(brush, bar);

        var label = Text("Paused", RulerTextSize, ThemeBrushes.Background);
        var box = Badge(x, label, PausedBadgeHorizontalPadding);
        context.FillRectangle(brush, box, BadgeCornerRadius);
        context.DrawText(label, new Point(box.X + PausedBadgeHorizontalPadding, box.Y + BadgeVerticalPadding));
    }

    /// <summary>
    /// Draws a dashed line where a drag starting here would snap to.
    /// It's a ghost of the selection's first edge.
    /// </summary>
    private void DrawSnapPreview(DrawingContext context, TimelineViewport viewport, TimelineViewModel timeline)
    {
        if (timeline.Selection.SnapPreviewUtc is not { } edge)
        {
            return;
        }

        var x = LineX(viewport, edge);
        context.DrawLine(ThemePens.SnapPreview, new Point(x, RulerHeight), new Point(x, Bounds.Height));
    }

    private void DrawHoverLine(DrawingContext context, TimelineViewport viewport, TimelineViewModel timeline)
    {
        if (timeline.HoverTimeUtc is not { } hover || !viewport.Visible.Contains(hover))
        {
            return;
        }

        var x = LineX(viewport, hover);
        context.DrawLine(ThemePens.Hover, new Point(x, RulerHeight), new Point(x, Bounds.Height));

        var label = Text(LocalTime.Format(hover, timeline.TimeZone), RulerTextSize, ThemeBrushes.Text);
        var box = Badge(x, label, BadgeHorizontalPadding);
        context.DrawRectangle(ThemeBrushes.RulerBackground, ThemePens.Hover, box, BadgeCornerRadius, BadgeCornerRadius);
        context.DrawText(label, new Point(box.X + BadgeHorizontalPadding, box.Y + BadgeVerticalPadding));
    }

    /// <summary>
    /// The box around a badge's label, centred on <paramref name="x"/> but kept inside the drawing area.
    /// </summary>
    private Rect Badge(double x, FormattedText label, double horizontalPadding)
    {
        var width = label.Width + horizontalPadding * 2;

        // Not Math.Clamp, which throws when the area is narrower than the badge, as it is before the first layout.
        var left = Math.Max(AreaLeft, Math.Min(x - width / 2, AreaLeft + AreaWidth - width));
        return new Rect(left, RulerLabelInset, width, label.Height + BadgeVerticalPadding * 2);
    }

    /// <summary>
    /// Shows the hover line at the pointer, or hides it when the pointer isn't over the drawing area.
    /// A drag captures the pointer, so its moves can come from outside the control.
    /// </summary>
    private void UpdateHover(TimelineViewModel timeline, Point point, KeyModifiers modifiers)
    {
        if (DrawingArea.Contains(point))
        {
            timeline.HoverAt(point.X - AreaLeft, snap: Snaps(modifiers));
        }
        else
        {
            timeline.EndHover();
        }
    }

    private double LaneTop(int index) => RulerHeight + index * LaneHeight;

    /// <summary>
    /// A cached single-line text.
    /// <paramref name="maxWidth"/> is set only when it changes, because setting it makes the text lay itself out again.
    /// </summary>
    private FormattedText Text(string text, double size, IBrush brush, double? maxWidth = null)
    {
        text = text.ReplaceLineEndings(" ");
        if (!_textCache.TryGetValue((text, size), out var formatted))
        {
            if (_textCache.Count > MaxCachedTexts)
            {
                _textCache.Clear();
            }

            formatted = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelFont,
                size,
                brush)
            {
                MaxLineCount = 1,
                Trimming = TextTrimming.CharacterEllipsis,
            };
            _textCache[(text, size)] = formatted;
        }

        formatted.SetForegroundBrush(brush);
        if (maxWidth is { } width && !FloatingPoint.AreEqual(formatted.MaxTextWidth, width))
        {
            formatted.MaxTextWidth = width;
        }

        return formatted;
    }

    /// <summary>
    /// The lane under <paramref name="y"/>, or -1 over the ruler.
    /// </summary>
    private int LaneAt(double y) => y < RulerHeight ? -1 : (int)((y - RulerHeight) / LaneHeight);

    /// <summary>
    /// Pens for the current theme, made once rather than on every frame.
    /// </summary>
    private sealed record Pens(
        IPen Grid,
        IPen Tick,
        IPen Selection,
        IPen Now,
        IPen Hover,
        IPen SelectedBlock,
        IPen SnapPreview)
    {
        public static Pens ForCurrentTheme() => new(
            Make(ThemeBrushes.GridLine),
            Make(ThemeBrushes.Tick),
            Make(ThemeBrushes.SelectionBorder, EmphasisPenThickness),
            Make(ThemeBrushes.NowLine, EmphasisPenThickness),
            Make(ThemeBrushes.HoverLine),
            Make(ThemeBrushes.SelectedBlock, SelectedBlockPenThickness),
            Make(ThemeBrushes.SelectionBorder, thickness: 1, dash: DashStyle.Dash));

        private static ImmutablePen Make(IBrush brush, double thickness = 1, IDashStyle? dash = null) =>
            new Pen(brush, thickness, dash).ToImmutable();
    }
}
