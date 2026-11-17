using System.Globalization;

using Avalonia.Media;

using ClockShot.App.Controls;

namespace ClockShot.App.Tests;

public sealed class ThemeBrushesTests
{
    [Fact]
    public void FromHexShowsAStoredColour()
    {
        // Act
        var brush = FromHex("#3E8E5E");

        // Assert
        Assert.Equal(Color.Parse("#3E8E5E"), Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color);
    }

    [Theory]
    [InlineData("not a colour")]
    [InlineData(null)]
    public void FromHexShowsAMissingOrInvalidColourAsGrey(string? hex)
    {
        // Act
        var brush = FromHex(hex);

        // Assert
        Assert.Equal(Colors.Gray, Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color);
    }

    private static object? FromHex(string? hex) =>
        ThemeBrushes.FromHex.Convert(hex, typeof(IBrush), parameter: null, CultureInfo.InvariantCulture);
}
