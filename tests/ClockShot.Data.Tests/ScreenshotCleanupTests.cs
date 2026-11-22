using System.Threading.Channels;

using ClockShot.Core.Screenshots;
using ClockShot.Core.Settings;
using ClockShot.Data.Settings;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.Extensions;

namespace ClockShot.Data.Tests;

public sealed class ScreenshotCleanupTests : IAsyncDisposable
{
    private static readonly DateTime Start = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly JumpingClock _clock = new(Start);
    private readonly IScreenshotStore _screenshots = Substitute.For<IScreenshotStore>();
    private readonly Channel<DateTime> _deleteCutoffs = Channel.CreateUnbounded<DateTime>();
    private readonly JsonSettingsStore _settings = MemorySettingsStore.Create();
    private readonly ScreenshotCleanup _cleanup;

    public ScreenshotCleanupTests()
    {
        // Deletes run in the background, so the tests await each delete's cutoff.
        _screenshots
            .When(store => store.DeleteTakenBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
            .Do(call => _deleteCutoffs.Writer.TryWrite(call.Arg<DateTime>()));

        _settings.Save(new AppSettings { Screenshots = new ScreenshotSettings { KeepForDays = 7 } });
        _cleanup = new ScreenshotCleanup(_screenshots, _settings, _clock, NullLogger<ScreenshotCleanup>.Instance);
    }

    public ValueTask DisposeAsync() => _cleanup.DisposeAsync();

    [Fact]
    public async Task DeletesScreenshotsOlderThanTheSettingAtStart()
    {
        // Act
        _cleanup.Start();

        // Assert
        Assert.Equal(Start.AddDays(-7), await NextCutoffAsync());
    }

    [Fact]
    public async Task DeletesAgainAnHourLater()
    {
        // Arrange
        _cleanup.Start();
        await NextCutoffAsync();

        // Act
        _clock.Advance(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(Start.AddHours(1).AddDays(-7), await NextCutoffAsync());
    }

    [Fact]
    public async Task AFailedCleanupDoesNotStopTheNextOne()
    {
        // Arrange
        // Configure keeps this setup from counting as a delete.
        _screenshots.Configure()
            .DeleteTakenBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new IOException("The disk is busy.")));
        _cleanup.Start();
        await NextCutoffAsync();

        // Act
        _clock.Advance(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(Start.AddHours(1).AddDays(-7), await NextCutoffAsync());
    }

    [Fact]
    public async Task IgnoresTheClockJumpingForward()
    {
        // Arrange
        _cleanup.Start();
        await NextCutoffAsync();

        // Act
        _clock.Jump = TimeSpan.FromDays(40);
        _clock.Advance(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(Start.AddHours(1).AddDays(-7), await NextCutoffAsync());
    }

    [Fact]
    public async Task FollowsTheClockMovedBack()
    {
        // Arrange
        _cleanup.Start();
        await NextCutoffAsync();

        // Act
        _clock.Jump = TimeSpan.FromDays(-40);
        _clock.Advance(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(Start.AddHours(1).AddDays(-40).AddDays(-7), await NextCutoffAsync());
    }

    private Task<DateTime> NextCutoffAsync() => BackgroundWork.ReadNextAsync(_deleteCutoffs);

    /// <summary>
    /// A clock that can be moved while the elapsed time and timers carry on.
    /// </summary>
    private sealed class JumpingClock(DateTime start) : FakeTimeProvider(start)
    {
        public TimeSpan Jump { get; set; }

        public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + Jump;

        // The base class counts its timestamps in ticks from GetUtcNow, which would include the jump.
        public override long GetTimestamp() => base.GetUtcNow().Ticks;
    }
}
