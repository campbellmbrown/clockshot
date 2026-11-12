using ClockShot.Core;

namespace ClockShot.App.Tests;

public sealed class SingleInstanceTests
{
    /// <summary>
    /// Only a limit for a broken listener. The first copy is normally asked within milliseconds.
    /// </summary>
    private static readonly TimeSpan ShowWait = TimeSpan.FromSeconds(10);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"{AppInfo.FilePrefix}-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void TheFirstCopyForAFolderKeepsRunning()
    {
        // Act
        using var first = new SingleInstance(_folder);

        // Assert
        Assert.True(first.IsFirst);
    }

    [Fact]
    public void ASecondCopyForTheSameFolderIsNotFirst()
    {
        // Arrange
        using var first = new SingleInstance(_folder);

        // Act
        using var second = new SingleInstance(_folder);

        // Assert
        Assert.False(second.IsFirst);
    }

    [Fact]
    public void TheSameFolderIgnoresCase()
    {
        // Arrange
        using var first = new SingleInstance(_folder);

        // Act
        using var second = new SingleInstance(_folder.ToUpperInvariant());

        // Assert
        Assert.False(second.IsFirst);
    }

    [Fact]
    public void TheSameFolderIgnoresATrailingSeparator()
    {
        // Arrange
        using var first = new SingleInstance(_folder);

        // Act
        using var second = new SingleInstance(_folder + Path.DirectorySeparatorChar);

        // Assert
        Assert.False(second.IsFirst);
    }

    [Fact]
    public void CopiesForDifferentFoldersBothKeepRunning()
    {
        // Arrange
        using var first = new SingleInstance(_folder);

        // Act
        using var other = new SingleInstance(_folder + "-other");

        // Assert
        Assert.True(other.IsFirst);
    }

    [Fact]
    public void TheFolderIsFreeAgainOnceTheFirstCopyEnds()
    {
        // Arrange
        new SingleInstance(_folder).Dispose();

        // Act
        using var next = new SingleInstance(_folder);

        // Assert
        Assert.True(next.IsFirst);
    }

    [Fact]
    public void TheFirstCopyCanEndOnAnotherThread()
    {
        // Arrange
        var first = new SingleInstance(_folder);

        // Act
        var cancel = TestContext.Current.CancellationToken;
        var error = Record.Exception(() => Task.Run(first.Dispose, cancel).Wait(cancel));

        // Assert
        Assert.Null(error);
        using var next = new SingleInstance(_folder);
        Assert.True(next.IsFirst);
    }

    [Fact]
    public void ASecondCopyAsksTheFirstToShowItsWindow()
    {
        // Arrange
        using var shown = new ManualResetEventSlim();
        using var first = new SingleInstance(_folder);
        first.ListenForLaunches(shown.Set);
        using var second = new SingleInstance(_folder);

        // Act
        second.AskRunningCopyToShow();

        // Assert
        Assert.True(shown.Wait(ShowWait, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void AskingGivesUpQuietlyWhenTheFirstCopyIsNotListening()
    {
        // Arrange
        using var first = new SingleInstance(_folder);

        // The first copy isn't listening, so waiting for an answer would only slow the test down.
        using var second = new SingleInstance(_folder, connectTimeout: TimeSpan.Zero);

        // Act
        var error = Record.Exception(second.AskRunningCopyToShow);

        // Assert
        Assert.Null(error);
    }
}
