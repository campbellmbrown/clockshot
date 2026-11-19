using ClockShot.Core.Projects;

namespace ClockShot.Core.Tests.Projects;

public sealed class SearchWordsTests
{
    [Fact]
    public void MatchesEveryWordInAnyOrderIgnoringCase()
    {
        // Arrange
        var words = SearchWords.Of("WEB client");

        // Act
        var matches = words.AreAllIn("Client A > Website");

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void DoesNotMatchWhenAWordIsMissing()
    {
        // Arrange
        var words = SearchWords.Of("client shop");

        // Act
        var matches = words.AreAllIn("Client A > Website");

        // Assert
        Assert.False(matches);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankTextIsEmptyAndMatchesEverything(string? text)
    {
        // Act
        var words = SearchWords.Of(text);

        // Assert
        Assert.True(words.IsEmpty);
        Assert.True(words.AreAllIn("Website"));
    }
}
