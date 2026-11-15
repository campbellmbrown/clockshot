using Microsoft.Extensions.Logging;

namespace ClockShot.App.Services;

/// <summary>
/// Starts work without waiting for it.
/// Unlike <c>_ = WorkAsync()</c>, it logs a failure instead of losing it.
/// </summary>
public sealed class BackgroundWork(ILogger<BackgroundWork> logger)
{
    public void Run(Func<Task> work) => _ = RunAsync(work);

    /// <summary>
    /// Never throws, so the task it returns can be ignored.
    /// </summary>
    private async Task RunAsync(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background work failed. The app carries on.");
        }
    }
}
