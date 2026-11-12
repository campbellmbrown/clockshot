using System.Globalization;
using System.Reflection;
using System.Xml.Linq;

using ClockShot.App.Controls;

namespace ClockShot.App.Tests;

/// <summary>
/// Checks the icon files against <see cref="Icons"/> and the drawing rules in <see cref="IconGeometry"/>.
/// A broken icon then fails here instead of on screen.
/// </summary>
public sealed class IconsTests
{
    private static readonly string IconFolder = Path.Combine(RepositoryFolder.Root, "src", "ClockShot.App", "Assets", "Icons");

    [Fact]
    public void EveryIconHasAnSvg()
    {
        // Arrange
        var iconNames = IconNames();
        var svgNames = SvgFiles().Select(svg => svg.Name);

        // Act
        var iconsWithoutSvg = iconNames.Except(svgNames).ToList();

        // Assert
        Assert.Empty(iconsWithoutSvg);
    }

    [Fact]
    public void EverySvgHasAnIcon()
    {
        // Arrange
        var iconNames = IconNames();
        var svgNames = SvgFiles().Select(svg => svg.Name);

        // Act
        var svgsWithoutIcon = svgNames.Except(iconNames).ToList();

        // Assert
        Assert.Empty(svgsWithoutIcon);
    }

    [Fact]
    public void EverySvgUsesTheIconGrid()
    {
        // Arrange
        var grid = IconGeometry.GridSize.ToString(CultureInfo.InvariantCulture);

        // Act
        var offGrid = SvgFiles().Where(svg => (string?)svg.Root.Attribute("viewBox") != $"0 0 {grid} {grid}").ToList();

        // Assert
        Assert.Empty(offGrid.Select(svg => svg.Name));
    }

    [Fact]
    public void EverySvgUsesTheIconStrokeWidth()
    {
        // Arrange
        var strokeWidth = IconGeometry.StrokeWidth.ToString(CultureInfo.InvariantCulture);

        // Act
        var wrongWidth = SvgFiles().Where(svg => (string?)svg.Root.Attribute("stroke-width") != strokeWidth).ToList();

        // Assert
        Assert.Empty(wrongWidth.Select(svg => svg.Name));
    }

    [Fact]
    public void EverySvgUsesOnlySupportedElements()
    {
        // Act
        var unsupported = SvgFiles()
            .Where(svg => svg.Root.Elements().Any(element => !IconGeometry.SupportedElements.Contains(element.Name.LocalName)))
            .ToList();

        // Assert
        Assert.Empty(unsupported.Select(svg => svg.Name));
    }

    private static IEnumerable<string> IconNames() =>
        typeof(Icons).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!);

    private static IEnumerable<Svg> SvgFiles() =>
        Directory.EnumerateFiles(IconFolder, "*.svg")
            .Select(path => new Svg(Path.GetFileNameWithoutExtension(path), XDocument.Load(path).Root!));

    private sealed record Svg(string Name, XElement Root);
}
