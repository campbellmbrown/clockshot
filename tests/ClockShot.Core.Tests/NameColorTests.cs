using ClockShot.Core.Models;

namespace ClockShot.Core.Tests;

public sealed class NameColorTests
{
    [Theory]
    [InlineData("Editor", "#B2CB4D")]
    [InlineData("Browser", "#CB5C4D")]
    [InlineData("Team Chat", "#B64DCB")]
    public void KnownNamesKeepTheirColours(string name, string expected)
    {
        // Act
        var colour = NameColor.For(name);

        // Assert
        Assert.Equal(expected, colour);
    }

    [Fact]
    public void TheCaseOfANameDoesNotChangeItsColour()
    {
        // Act
        var colour = NameColor.For("EDITOR");

        // Assert
        Assert.Equal(NameColor.For("Editor"), colour);
    }

    [Fact]
    public void TheColourIsAHashSignAndSixHexDigits()
    {
        // Act
        var colour = NameColor.For("Team Chat");

        // Assert
        Assert.Matches("^#[0-9A-F]{6}$", colour);
    }
}
