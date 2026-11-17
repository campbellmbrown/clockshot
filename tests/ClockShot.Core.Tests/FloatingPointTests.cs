namespace ClockShot.Core.Tests;

public sealed class FloatingPointTests
{
    [Fact]
    public void ValuesThatDifferOnlyByRoundingAreEqual()
    {
        // Arrange
        var sum = 0.1 + 0.2;

        // Act
        var areEqual = FloatingPoint.AreEqual(sum, 0.3);

        // Assert
        Assert.NotEqual(0.3, sum);
        Assert.True(areEqual);
    }

    [Fact]
    public void ValuesThatReallyDifferAreNotEqual()
    {
        // Act
        var areEqual = FloatingPoint.AreEqual(100, 100.000001);

        // Assert
        Assert.False(areEqual);
    }
}
