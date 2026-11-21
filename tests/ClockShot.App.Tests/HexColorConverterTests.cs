using System.Globalization;

using Avalonia.Media;

using ClockShot.App.Controls;

namespace ClockShot.App.Tests;

public sealed class HexColorConverterTests
{
    [Fact]
    public void AColourComesBackAsTheSameText()
    {
        // Arrange
        var converter = HexColorConverter.Instance;

        // Act
        var color = converter.Convert("#C0453F", typeof(Color), parameter: null, CultureInfo.InvariantCulture);
        var text = converter.ConvertBack(color, typeof(string), parameter: null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("#C0453F", text);
    }

    [Fact]
    public void TextThatIsNotAColourShowsAsGrey()
    {
        // Act
        var color = HexColorConverter.Instance.Convert("not a colour", typeof(Color), parameter: null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Colors.Gray, color);
    }
}
