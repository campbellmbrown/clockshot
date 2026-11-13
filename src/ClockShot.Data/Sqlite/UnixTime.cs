using ClockShot.Core.Models;

namespace ClockShot.Data.Sqlite;

/// <summary>
/// Converts between UTC <see cref="DateTime"/> and the Unix milliseconds stored in the database.
/// </summary>
internal static class UnixTime
{
    public static long ToMilliseconds(DateTime utc) => new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeMilliseconds();

    public static DateTime FromMilliseconds(long milliseconds) =>
        DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;

    public static TimeRange ToRange(long startMilliseconds, long endMilliseconds) =>
        new(FromMilliseconds(startMilliseconds), FromMilliseconds(endMilliseconds));
}
