using ClockShot.App.Controls;
using ClockShot.App.ViewModels;
using ClockShot.Core;
using ClockShot.Core.Tracking;

namespace ClockShot.App.Tests.ViewModels;

public sealed class TrackingStatusViewModelTests
{
    private static readonly TrackingStatus Running = new(IsPaused: false, State: null, Window: null);

    [Fact]
    public void ThePauseCommandPausesWhileRunning()
    {
        // Arrange
        var tracking = new FakeTrackingController(Running);
        var viewModel = new TrackingStatusViewModel(tracking);

        // Act
        viewModel.TogglePausedCommand.Execute(null);

        // Assert
        Assert.True(tracking.Status.IsPaused);
    }

    [Fact]
    public void ThePauseCommandResumesWhilePaused()
    {
        // Arrange
        var tracking = new FakeTrackingController(TrackingStatus.Paused);
        var viewModel = new TrackingStatusViewModel(tracking);

        // Act
        viewModel.TogglePausedCommand.Execute(null);

        // Assert
        Assert.False(tracking.Status.IsPaused);
    }

    [Fact]
    public void OffersToPauseWhileRunning()
    {
        // Act
        var viewModel = new TrackingStatusViewModel(new FakeTrackingController(Running));

        // Assert
        Assert.Equal("Pause tracking", viewModel.PauseCommandText);
        Assert.Equal(Icons.PauseTracking, viewModel.PauseCommandIconKind);
        Assert.Equal($"{AppInfo.Name}: tracking", viewModel.StatusText);
    }

    [Fact]
    public void OffersToResumeWhilePaused()
    {
        // Act
        var viewModel = new TrackingStatusViewModel(new FakeTrackingController(TrackingStatus.Paused));

        // Assert
        Assert.Equal("Resume tracking", viewModel.PauseCommandText);
        Assert.Equal(Icons.ResumeTracking, viewModel.PauseCommandIconKind);
        Assert.Equal($"{AppInfo.Name}: tracking paused", viewModel.StatusText);
    }
}
