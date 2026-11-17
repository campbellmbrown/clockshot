using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Timeline;

namespace ClockShot.App.Controls;

/// <summary>
/// Draws the timeline that <see cref="TimelineViewModel"/> describes.
/// </summary>
public sealed class TimelineControl : Control
{
    public const double LaneTitleWidth = 110;
    public const double RulerHeight = 28;

    public static readonly StyledProperty<TimelineViewModel?> TimelineProperty =
        AvaloniaProperty.Register<TimelineControl, TimelineViewModel?>(nameof(Timeline));

    // Text sizes are in pixels.
    private const double RulerTextSize = 11;

    private const double MajorTickHeight = 10;
    private const double MinorTickHeight = 4;

    // How far ruler labels sit from their tick and from the top of the ruler.
    private const double RulerLabelInset = 4;

    // This is many times the number of labels in one day.
    // Past it the cache is cleared, so it can't grow without limit.
    private const int MaxCachedTexts = 2000;

    // Before its first layout the control is no wider than the lane titles, which would leave no drawing area.
    private const double MinAreaWidth = 1;

    private static readonly Typeface LabelFont = new(FontFamily.Default);

    private readonly Dictionary<(string Text, double Size), FormattedText> _textCache = [];
    private Pens? _pens;

    public TimelineControl()
    {
        ClipToBounds = true;
        ActualThemeVariantChanged += (_, _) =>
        {
            // The pens are made again from the new theme's brushes on the next draw.
            _pens = null;
            InvalidateVisual();
        };
    }

    public TimelineViewModel? Timeline
    {
        get => GetValue(TimelineProperty);
        set => SetValue(TimelineProperty, value);
    }

    /// <summary>
    /// The x of the drawing area's left edge, after the lane titles.
    /// </summary>
    private static double AreaLeft => LaneTitleWidth;

    private double AreaWidth => Math.Max(MinAreaWidth, Bounds.Width - AreaLeft);

    private Pens ThemePens => _pens ??= Pens.ForCurrentTheme();

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(ThemeBrushes.Background, new Rect(Bounds.Size));
        if (Timeline is not { } timeline)
        {
            return;
        }

        var viewport = timeline.Zoom.Viewport;

        DrawLaneBackgrounds(context);
        using (context.PushClip(new Rect(AreaLeft, 0, AreaWidth, Bounds.Height)))
        {
            DrawRuler(context, viewport, timeline.TimeZone);
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

            InvalidateVisual();
        }
        else if (change.Property == BoundsProperty && Timeline is { } timeline)
        {
            ReportWidth(timeline);
        }
    }

    /// <summary>
    /// The x of a time on the control, centred on a pixel so a one-pixel line is drawn sharp.
    /// </summary>
    private static double LineX(TimelineViewport viewport, DateTime time) =>
        Math.Round(AreaLeft + viewport.XOf(time)) + 0.5;

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

    private void OnRedrawNeeded() => InvalidateVisual();

    private void DrawLaneBackgrounds(DrawingContext context)
    {
        context.FillRectangle(ThemeBrushes.RulerBackground, new Rect(0, 0, Bounds.Width, RulerHeight));

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
    /// A cached single-line text.
    /// </summary>
    private FormattedText Text(string text, double size, IBrush brush)
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
        return formatted;
    }

    /// <summary>
    /// Pens for the current theme, made once rather than on every frame.
    /// </summary>
    private sealed record Pens(
        IPen Grid,
        IPen Tick)
    {
        public static Pens ForCurrentTheme() => new(
            Make(ThemeBrushes.GridLine),
            Make(ThemeBrushes.Tick));

        private static ImmutablePen Make(IBrush brush, double thickness = 1, IDashStyle? dash = null) =>
            new Pen(brush, thickness, dash).ToImmutable();
    }
}
