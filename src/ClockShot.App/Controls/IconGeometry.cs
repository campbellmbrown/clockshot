using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Xml.Linq;

using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace ClockShot.App.Controls;

/// <summary>
/// One shape of an icon. <paramref name="IsFilled"/> fills the shape as well as outlining it.
/// </summary>
public sealed record IconShape(Geometry Geometry, bool IsFilled);

/// <summary>
/// Reads the icon SVGs into shapes and draws them.
/// The icons are outlines. Avalonia's PathIcon only fills shapes, so it can't draw them.
/// </summary>
/// <remarks>
/// Any SVG that follows these rules works.
/// <list type="bullet">
/// <item>The viewBox is <see cref="GridSize"/> by <see cref="GridSize"/>.</item>
/// <item>The stroke width is <see cref="StrokeWidth"/>.</item>
/// <item>It uses only the <see cref="SupportedElements"/>, with no groups or transforms.</item>
/// <item><c>fill="currentColor"</c> fills a shape in the text colour.</item>
/// </list>
/// Any other element throws when the icon loads.
/// IconsTests checks every file.
/// </remarks>
public static class IconGeometry
{
    /// <summary>
    /// The size of the grid that icons are drawn on. It matches the SVG viewBox.
    /// </summary>
    public const double GridSize = 24;

    /// <summary>
    /// The pen width in grid units. Lines get thinner as an icon gets smaller.
    /// </summary>
    public const double StrokeWidth = 2;

    public static readonly FrozenSet<string> SupportedElements = FrozenSet.Create("path", "circle", "ellipse", "rect", "line");

    private const string IconFolder = "avares://ClockShot/Assets/Icons/";
    private static readonly ConcurrentDictionary<string, IReadOnlyList<IconShape>> Cache = new();

    public static bool TryLoad(string name, out IReadOnlyList<IconShape> shapes)
    {
        if (Cache.TryGetValue(name, out var cached))
        {
            shapes = cached;
            return true;
        }
        var uri = UriOf(name);
        if (!AssetLoader.Exists(uri))
        {
            shapes = [];
            return false;
        }
        shapes = Cache.GetOrAdd(name, _ => Parse(uri));
        return true;
    }

    /// <summary>
    /// Draws the shapes as large as fits in <paramref name="area"/>, centred.
    /// </summary>
    public static void Draw(DrawingContext context, IReadOnlyList<IconShape> shapes, IBrush brush, Rect area)
    {
        var size = Math.Min(area.Width, area.Height);
        var scale = size / GridSize;
        var offset = area.Center - new Point(size / 2, size / 2);
        var pen = new Pen(brush, StrokeWidth, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(offset)))
        {
            foreach (var shape in shapes)
            {
                context.DrawGeometry(shape.IsFilled ? brush : null, pen, shape.Geometry);
            }
        }
    }

    private static Uri UriOf(string name) => new(IconFolder + name + ".svg");

    private static IReadOnlyList<IconShape> Parse(Uri uri)
    {
        using var stream = AssetLoader.Open(uri);
        return [.. XDocument.Load(stream).Root!.Elements().Select(element => ToShape(element, uri))];
    }

    private static IconShape ToShape(XElement element, Uri uri)
    {
        Geometry geometry = element.Name.LocalName switch
        {
            "path" => Geometry.Parse((string)element.Attribute("d")!),
            "circle" => Ellipse(element, Number(element, "r"), Number(element, "r")),
            "ellipse" => Ellipse(element, Number(element, "rx"), Number(element, "ry")),
            "line" => new LineGeometry(
                new Point(Number(element, "x1"), Number(element, "y1")),
                new Point(Number(element, "x2"), Number(element, "y2"))),
            "rect" => Rectangle(element),
            var other => throw new InvalidDataException($"{uri} uses <{other}>, which icons can't use (see IconGeometry)."),
        };
        return new IconShape(geometry, (string?)element.Attribute("fill") == "currentColor");
    }

    private static EllipseGeometry Ellipse(XElement element, double radiusX, double radiusY) =>
        new() { Center = new Point(Number(element, "cx"), Number(element, "cy")), RadiusX = radiusX, RadiusY = radiusY };

    private static RectangleGeometry Rectangle(XElement element)
    {
        var bounds = new Rect(Number(element, "x"), Number(element, "y"), Number(element, "width"), Number(element, "height"));
        var radiusX = Number(element, "rx");
        var radiusY = element.Attribute("ry") is null ? radiusX : Number(element, "ry");
        return new RectangleGeometry(bounds, radiusX, radiusY);
    }

    /// <summary>
    /// A numeric attribute. SVG treats a missing one as 0.
    /// </summary>
    private static double Number(XElement element, string attribute) =>
        element.Attribute(attribute) is { } value ? double.Parse(value.Value, CultureInfo.InvariantCulture) : 0;
}
