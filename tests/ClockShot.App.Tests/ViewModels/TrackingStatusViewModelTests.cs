using System.IO.Abstractions.TestingHelpers;

using ClockShot.App.Controls;
using ClockShot.App.ViewModels;
using ClockShot.Core;
using ClockShot.Core.Screenshots;
using ClockShot.Core.Tracking;
using ClockShot.Data;
using ClockShot.Data.Screenshots;
using ClockShot.Data.Settings;
using ClockShot.Data.Tracking;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace ClockShot.App.Tests.ViewModels;

public sealed class TrackingStatusViewModelTests
{
    private readonly TrackingLoop _tracking = UnstartedLoop();

    [Fact]
    public void ThePauseCommandPauses()
    {
        // Arrange
        var viewModel = new TrackingStatusViewModel(_tracking);

        // Act
        viewModel.TogglePausedCommand.Execute(null);

        // Assert
        Assert.True(_tracking.Status.IsPaused);
    }

    [Fact]
    public void ThePauseCommandResumesWhilePaused()
    {
        // Arrange
        _tracking.Pause();
        var viewModel = new TrackingStatusViewModel(_tracking);

        // Act
        viewModel.TogglePausedCommand.Execute(null);

        // Assert
        Assert.False(_tracking.Status.IsPaused);
    }

    [Fact]
    public void OffersToPause()
    {
        // Act
        var viewModel = new TrackingStatusViewModel(_tracking);

        // Assert
        Assert.Equal("Pause tracking", viewModel.PauseCommandText);
        Assert.Equal(Icons.PauseTracking, viewModel.PauseCommandIconKind);
        Assert.Equal($"{AppInfo.Name}: tracking", viewModel.StatusText);
    }

    [Fact]
    public void OffersToResumeWhilePaused()
    {
        // Arrange
        _tracking.Pause();

        // Act
        var viewModel = new TrackingStatusViewModel(_tracking);

        // Assert
        Assert.Equal("Resume tracking", viewModel.PauseCommandText);
        Assert.Equal(Icons.ResumeTracking, viewModel.PauseCommandIconKind);
        Assert.Equal($"{AppInfo.Name}: tracking paused", viewModel.StatusText);
    }

    /// <summary>
    /// Pausing and resuming work without starting the loop.
    /// Unstarted, it never samples, captures, saves or reads the settings.
    /// </summary>
    private static TrackingLoop UnstartedLoop()
    {
        var files = new MockFileSystem();
        var paths = new DataPaths("data");
        var database = UnopenedDatabase.Create();

        return new TrackingLoop(
            Substitute.For<ISystemSampler>(),
            Substitute.For<IWindowCapturer>(),
            new TrackingWriter(database, NullLogger<TrackingWriter>.Instance),
            new ScreenshotStore(files, paths, database, TimeProvider.System),
            new JsonSettingsStore(files, paths, NullLogger<JsonSettingsStore>.Instance),
            TimeProvider.System,
            NullLogger<TrackingLoop>.Instance);
    }
}
