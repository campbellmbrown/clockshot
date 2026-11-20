using System.Threading.Channels;

namespace ClockShot.Data.Tests;

/// <summary>
/// Waits for what code under test writes from another thread.
/// </summary>
internal static class BackgroundWork
{
    // Only broken code waits this long. Without a limit, it would hang the test run.
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    public static async Task<T> ReadNextAsync<T>(Channel<T> channel)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(Limit);
        return await channel.Reader.ReadAsync(timeout.Token);
    }
}
