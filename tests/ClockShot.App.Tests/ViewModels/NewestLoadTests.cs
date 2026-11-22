using ClockShot.App.ViewModels;

namespace ClockShot.App.Tests.ViewModels;

public sealed class NewestLoadTests
{
    [Fact]
    public void StartingALoadCancelsThePreviousOne()
    {
        // Arrange
        using var newestLoad = new NewestLoad();
        var first = newestLoad.Start();

        // Act
        var second = newestLoad.Start();

        // Assert
        Assert.True(first.IsCancellationRequested);
        Assert.False(second.IsCancellationRequested);
    }

    [Fact]
    public void CancelStopsTheCurrentLoad()
    {
        // Arrange
        using var newestLoad = new NewestLoad();
        var load = newestLoad.Start();

        // Act
        newestLoad.Cancel();

        // Assert
        Assert.True(load.IsCancellationRequested);
    }

    [Fact]
    public void DisposingCancelsTheCurrentLoad()
    {
        // Arrange
        var newestLoad = new NewestLoad();
        var load = newestLoad.Start();

        // Act
        newestLoad.Dispose();

        // Assert
        Assert.True(load.IsCancellationRequested);
    }

    [Fact]
    public void ALoadStartedAfterDisposingIsAlreadyCancelled()
    {
        // Arrange
        var newestLoad = new NewestLoad();
        newestLoad.Start();   // gives it a source for Dispose to dispose
        newestLoad.Dispose();

        // Act
        var load = newestLoad.Start();

        // Assert
        Assert.True(load.IsCancellationRequested);
    }

    [Fact]
    public void CancellingAfterDisposingDoesNothing()
    {
        // Arrange
        var newestLoad = new NewestLoad();
        newestLoad.Start();   // gives it a source for Dispose to dispose
        newestLoad.Dispose();

        // Act
        var exception = Record.Exception(newestLoad.Cancel);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void CancelAndDisposeWorkBeforeAnyLoad()
    {
        // Arrange
        var newestLoad = new NewestLoad();

        // Act
        var exception = Record.Exception(() =>
        {
            newestLoad.Cancel();
            newestLoad.Dispose();
        });

        // Assert
        Assert.Null(exception);
    }
}
