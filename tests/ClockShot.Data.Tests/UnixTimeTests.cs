using ClockShot.Core.Models;
using ClockShot.Data.Sqlite;

namespace ClockShot.Data.Tests;

public sealed class UnixTimeTests
{
    [Fact]
    public void TheUnixEpochIsZero()
    {
        // Act
        var milliseconds = UnixTime.ToMilliseconds(DateTime.UnixEpoch);

        // Assert
        Assert.Equal(0, milliseconds);
    }

    [Fact]
    public void ATimeComesBackUnchangedAndInUtc()
    {
        // Arrange
        var time = new DateTime(2026, 11, 10, 9, 30, 5, 120, DateTimeKind.Utc);

        // Act
        var readBack = UnixTime.FromMilliseconds(UnixTime.ToMilliseconds(time));

        // Assert
        Assert.Equal(time, readBack);
        Assert.Equal(DateTimeKind.Utc, readBack.Kind);
    }

    [Fact]
    public void ToRangeConvertsBothEnds()
    {
        // Act
        var range = UnixTime.ToRange(startMilliseconds: 0, endMilliseconds: 1000);

        // Assert
        Assert.Equal(new TimeRange(DateTime.UnixEpoch, DateTime.UnixEpoch.AddSeconds(1)), range);
    }
}
