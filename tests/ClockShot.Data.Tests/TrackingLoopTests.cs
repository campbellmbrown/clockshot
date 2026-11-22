using System.Collections.Concurrent;
using System.Threading.Channels;

using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;
using ClockShot.Core.Settings;
using ClockShot.Core.Tracking;
using ClockShot.Data.Settings;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.Extensions;

namespace ClockShot.Data.Tests;

/// <summary>
/// Runs the real loop on a fake clock.
/// The tests await what the loop writes, because the loop runs on another thread.
/// </summary>
public sealed class TrackingLoopTests : IAsyncDisposable
{
    private static readonly DateTime StartTime = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private const nint AnotherHandle = 99;

    private static readonly ForegroundWindow Editor = new(
        Handle: 1,
        ProcessName: "Editor.exe",
        ExecutablePath: null,
        DisplayName: "Editor",
        Title: "a.cs");

    private static readonly CapturedImage Image = new(width: 1, height: 1, new byte[CapturedImage.BytesPerPixel]);

    // Longer than the default away setting of 5 minutes.
    private static readonly TimeSpan LongIdle = TimeSpan.FromMinutes(10);

    private readonly FakeTimeProvider _clock = new(StartTime);
    private readonly ISystemSampler _sampler = Substitute.For<ISystemSampler>();
    private readonly IWindowCapturer _capturer = Substitute.For<IWindowCapturer>();
    private readonly ITrackingSink _sink = Substitute.For<ITrackingSink>();
    private readonly Channel<IReadOnlyList<TrackingChange>> _writes = Channel.CreateUnbounded<IReadOnlyList<TrackingChange>>();
    private readonly IScreenshotStore _screenshots = Substitute.For<IScreenshotStore>();
    private readonly ConcurrentQueue<DateTime> _savedTimes = new();
    private readonly JsonSettingsStore _settings = MemorySettingsStore.Create();
    private readonly TrackingLoop _loop;

    // The sampler reports these, and a test may change them between ticks.
    private ForegroundWindow _window = Editor;
    private TimeSpan _idleTime;

    public TrackingLoopTests()
    {
        _sampler.Read(Arg.Any<DateTime>())
            .Returns(call => new TrackingSample(call.Arg<DateTime>(), _window, _idleTime, IsLocked: false));
        _capturer.Capture(Arg.Any<nint>()).Returns(Image);
        _sink
            .When(sink => sink.Write(Arg.Any<IReadOnlyList<TrackingChange>>()))
            .Do(call => _writes.Writer.TryWrite(call.Arg<IReadOnlyList<TrackingChange>>()));
        _screenshots
            .When(store => store.SaveAsync(
                Arg.Any<CapturedImage>(),
                Arg.Any<DateTime>(),
                Arg.Any<ScreenshotEncoding>(),
                Arg.Any<CancellationToken>()))
            .Do(call => _savedTimes.Enqueue(call.Arg<DateTime>()));

        _loop = new TrackingLoop(
            _sampler,
            _capturer,
            _sink,
            _screenshots,
            _settings,
            _clock,
            NullLogger<TrackingLoop>.Instance);
    }

    public ValueTask DisposeAsync() => _loop.DisposeAsync();

    [Fact]
    public async Task FirstTickRecordsTheWindowAtStart()
    {
        // Act
        var changes = await StartAsync();

        // Assert
        Assert.Equal(
            [
                new StateStarted(StartTime, ComputerState.Active),
                new ActivityStarted(StartTime, Editor),
                new SegmentsExtended(StartTime),
            ],
            changes);
    }

    [Fact]
    public async Task EachTickSamplesAtTheClockTime()
    {
        // Arrange
        await StartAsync();

        // Act
        var changes = await TickAsync();

        // Assert
        Assert.Equal([new SegmentsExtended(At(1))], changes);
    }

    [Fact]
    public async Task UsesTheAwaySetting()
    {
        // Arrange
        _settings.Save(new AppSettings { Tracking = new TrackingSettings { AwayAfterMinutes = 1 } });
        _idleTime = TimeSpan.FromMinutes(2);

        // Act
        var changes = await StartAsync();

        // Assert
        Assert.Contains(new StateStarted(StartTime.AddMinutes(-2), ComputerState.Away), changes);
    }

    [Fact]
    public async Task StatusShowsTheStateAndWindow()
    {
        // Arrange
        await StartAsync();

        // Act
        // Disposing waits for the loop, which publishes the status after it writes.
        await _loop.DisposeAsync();

        // Assert
        Assert.Equal(new TrackingStatus(IsPaused: false, ComputerState.Active, Editor), _loop.Status);
    }

    [Fact]
    public async Task StartingTheLoopStartsTheSink()
    {
        // Act
        await StartAsync();

        // Assert
        _sink.Received().Start();
    }

    [Fact]
    public async Task ANewHandleForTheSameWindowLeavesTheStatusAlone()
    {
        // Arrange
        await StartAsync();
        // Waiting for the next tick makes sure the first one has published its status.
        await TickAsync();
        var raised = 0;
        _loop.StatusChanged += () => raised++;
        _window = Editor with { Handle = AnotherHandle };

        // Act
        await TickAsync();
        await _loop.DisposeAsync();

        // Assert
        Assert.Equal(0, raised);
    }

    [Fact]
    public void PausingShowsInTheStatusAtOnce()
    {
        // Arrange
        var raised = 0;
        _loop.StatusChanged += () => raised++;

        // Act
        _loop.Pause();

        // Assert
        Assert.Equal(TrackingStatus.Paused, _loop.Status);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void PausingTwiceRaisesStatusChangedOnce()
    {
        // Arrange
        var raised = 0;
        _loop.StatusChanged += () => raised++;
        _loop.Pause();

        // Act
        _loop.Pause();

        // Assert
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ResumingShowsInTheStatusAtOnce()
    {
        // Arrange
        _loop.Pause();

        // Act
        _loop.Resume();

        // Assert
        Assert.False(_loop.Status.IsPaused);
    }

    [Fact]
    public void ResumingWhenNotPausedChangesNothing()
    {
        // Arrange
        var raised = 0;
        _loop.StatusChanged += () => raised++;

        // Act
        _loop.Resume();

        // Assert
        Assert.Equal(TrackingStatus.NotStarted, _loop.Status);
        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task PausingBeforeStartRecordsNothing()
    {
        // Arrange
        _loop.Pause();

        // Act
        _loop.Start();
        await _loop.DisposeAsync();

        // Assert
        // The only write is the one disposing makes, with nothing open to end.
        Assert.Empty(await NextWriteAsync());
    }

    [Fact]
    public async Task PausingEndsTheOpenSegmentsAtTheNextTick()
    {
        // Arrange
        await StartAsync();

        // Act
        _loop.Pause();
        var changes = await TickAsync();

        // Assert
        Assert.Equal([new ActivityEnded(At(1)), new StateEnded(At(1))], changes);
    }

    [Fact]
    public async Task ResumingStartsRecordingAgainAtTheNextTick()
    {
        // Arrange
        await StartAsync();
        _loop.Pause();
        await TickAsync();

        // Act
        _loop.Resume();
        var changes = await TickAsync();

        // Assert
        Assert.Equal(
            [
                new StateStarted(At(2), ComputerState.Active),
                new ActivityStarted(At(2), Editor),
                new SegmentsExtended(At(2)),
            ],
            changes);
    }

    [Fact]
    public async Task ComingBackRaisesReturnedFromAway()
    {
        // Arrange
        TimeRange? returned = null;
        _loop.ReturnedFromAway += away => returned = away;
        _idleTime = LongIdle;
        await StartAsync();

        // Act
        _idleTime = TimeSpan.Zero;
        await TickAsync();
        await _loop.DisposeAsync();

        // Assert
        Assert.Equal(new TimeRange(StartTime - LongIdle, At(1)), returned);
    }

    [Fact]
    public async Task ResumingAfterAPauseIsNotComingBack()
    {
        // Arrange
        var returned = false;
        _loop.ReturnedFromAway += _ => returned = true;
        _idleTime = LongIdle;
        await StartAsync();
        _loop.Pause();
        await TickAsync();

        // Act
        _loop.Resume();
        _idleTime = TimeSpan.Zero;
        await TickAsync();
        await _loop.DisposeAsync();

        // Assert
        Assert.False(returned);
    }

    [Fact]
    public async Task TakesAScreenshotOfTheForegroundWindow()
    {
        // Arrange
        await StartAsync();

        // Act
        await _loop.DisposeAsync();

        // Assert
        _capturer.Received(1).Capture(Arg.Any<nint>());
        _capturer.Received(1).Capture(Editor.Handle);
        Assert.Equal([StartTime], _savedTimes);
    }

    [Fact]
    public async Task RetriesAFailedCaptureAtTheNextSample()
    {
        // Arrange
        // The first capture fails, as for a window that can't be captured.
        _capturer.Capture(Arg.Any<nint>()).Returns(null, Image);
        await StartAsync();

        // Act
        await TickAsync();
        await _loop.DisposeAsync();

        // Assert
        Assert.Equal([At(1)], _savedTimes);
    }

    [Fact]
    public async Task WaitsForAScreenshotToSaveBeforeTakingAnother()
    {
        // Arrange
        var shortestInterval = SettingLimits.ScreenshotIntervalSeconds.Min;
        _settings.Save(new AppSettings { Screenshots = new ScreenshotSettings { IntervalSeconds = shortestInterval } });
        var saving = new TaskCompletionSource();
        // Configure keeps this setup from counting as a save.
        _screenshots.Configure()
            .SaveAsync(Arg.Any<CapturedImage>(), Arg.Any<DateTime>(), Arg.Any<ScreenshotEncoding>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await saving.Task.WaitAsync(call.Arg<CancellationToken>());
                return (Screenshot?)null;
            });
        var captures = 0;
        _capturer.Capture(Arg.Any<nint>()).Returns(_ =>
        {
            Interlocked.Increment(ref captures);
            return Image;
        });
        await StartAsync();

        // Act
        // The next screenshot is due one interval after the first.
        for (var second = 0; second < shortestInterval; second++)
        {
            await TickAsync();
        }

        // Waiting for the next tick makes sure the one before it has finished.
        await TickAsync();
        var capturesBeforeTheSave = Volatile.Read(ref captures);
        saving.SetResult();

        // Assert
        Assert.Equal(1, capturesBeforeTheSave);
    }

    [Fact]
    public async Task DisposingEndsTheOpenSegmentsAndCompletesTheSink()
    {
        // Arrange
        await StartAsync();

        // Act
        await _loop.DisposeAsync();

        // Assert
        Assert.Equal([new ActivityEnded(StartTime), new StateEnded(StartTime)], await NextWriteAsync());
        await _sink.Received().CompleteAsync();
    }

    private static DateTime At(int seconds) => StartTime.AddSeconds(seconds);

    private async Task<IReadOnlyList<TrackingChange>> StartAsync()
    {
        _loop.Start();
        return await NextWriteAsync();
    }

    private async Task<IReadOnlyList<TrackingChange>> TickAsync()
    {
        _clock.Advance(TimeSpan.FromSeconds(1));
        return await NextWriteAsync();
    }

    private Task<IReadOnlyList<TrackingChange>> NextWriteAsync() => BackgroundWork.ReadNextAsync(_writes);
}
