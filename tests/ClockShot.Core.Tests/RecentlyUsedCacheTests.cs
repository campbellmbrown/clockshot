namespace ClockShot.Core.Tests;

public sealed class RecentlyUsedCacheTests
{
    private const int Capacity = 2;

    private readonly RecentlyUsedCache<int, string> _cache = new(Capacity);

    [Fact]
    public void FindsWhatWasSet()
    {
        // Arrange
        _cache.Set(1, "one");

        // Act
        var found = _cache.Find(1);

        // Assert
        Assert.Equal("one", found);
    }

    [Fact]
    public void FindsNothingForAKeyThatWasNeverSet()
    {
        // Arrange
        _cache.Set(1, "one");

        // Act
        var found = _cache.Find(2);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void SettingPastTheCapacityDropsTheLeastRecentlyUsed()
    {
        // Arrange
        _cache.Set(1, "one");
        _cache.Set(2, "two");

        // Act
        _cache.Set(3, "three");

        // Assert
        Assert.Null(_cache.Find(1));
        Assert.Equal("two", _cache.Find(2));
        Assert.Equal("three", _cache.Find(3));
    }

    [Fact]
    public void FindingAnItemMakesItTheMostRecentlyUsed()
    {
        // Arrange
        _cache.Set(1, "one");
        _cache.Set(2, "two");
        _cache.Find(1);

        // Act
        _cache.Set(3, "three");

        // Assert
        Assert.Equal("one", _cache.Find(1));
        Assert.Null(_cache.Find(2));
    }

    [Fact]
    public void SettingAKeyAgainKeepsOneEntryWithTheNewValue()
    {
        // Arrange
        _cache.Set(2, "two");
        _cache.Set(1, "one");

        // Act
        _cache.Set(1, "one again");

        // Assert
        // A second entry for 1 would have filled the cache and dropped 2.
        Assert.Equal("one again", _cache.Find(1));
        Assert.Equal("two", _cache.Find(2));
    }

    [Fact]
    public void SettingAKeyAgainMakesItTheMostRecentlyUsed()
    {
        // Arrange
        _cache.Set(1, "one");
        _cache.Set(2, "two");
        _cache.Set(1, "one again");

        // Act
        _cache.Set(3, "three");

        // Assert
        Assert.Null(_cache.Find(2));
        Assert.Equal("one again", _cache.Find(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ACapacityBelowOneIsRefused(int capacity)
    {
        // Act
        var create = () => new RecentlyUsedCache<int, string>(capacity);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(create);
    }
}
