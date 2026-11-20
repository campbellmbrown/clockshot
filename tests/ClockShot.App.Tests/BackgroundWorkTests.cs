using ClockShot.App.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace ClockShot.App.Tests;

public sealed class BackgroundWorkTests
{
    private readonly FakeLogger<BackgroundWork> _logger = new();

    [Fact]
    public void RunsTheWork()
    {
        // Arrange
        var ran = false;

        // Act
        new BackgroundWork(_logger).Run(() =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(ran);
        Assert.Empty(_logger.Collector.GetSnapshot());
    }

    [Fact]
    public void LogsWorkThatFailsLater()
    {
        // Arrange
        var failure = new InvalidOperationException("The file is open in another app.");
        var finish = new TaskCompletionSource();

        // Act
        new BackgroundWork(_logger).Run(async () =>
        {
            await finish.Task;
            throw failure;
        });
        finish.SetResult();

        // Assert
        var entry = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(failure, entry.Exception);
    }

    [Fact]
    public void LogsWorkThatFailsStraightAway()
    {
        // Arrange
        var failure = new InvalidOperationException("No database.");

        // Act
        new BackgroundWork(_logger).Run(() => throw failure);

        // Assert
        Assert.Same(failure, Assert.Single(_logger.Collector.GetSnapshot()).Exception);
    }
}
