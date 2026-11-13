using System.Threading.Channels;

using ClockShot.Core.Models;
using ClockShot.Core.Settings;
using ClockShot.Core.Tests.Settings;
using ClockShot.Core.Tracking;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace ClockShot.Core.Tests.Tracking;

/// <summary>
/// Runs the real loop on a fake clock.
/// The tests await what the loop writes, because the loop runs on another thread.
/// </summary>
public sealed class TrackingLoopTests : IAsyncDisposable
{
    private static readonly DateTime StartTime = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new("Editor.exe", null, "Editor", "a.cs");

    private readonly FakeTimeProvider _clock = new(StartTime);
    private readonly FakeSampler _sampler = new();
    private readonly FakeSink _sink = new();
    private readonly FakeSettingsStore _settings = new();
    private readonly TrackingLoop _loop;

    public TrackingLoopTests()
    {
        _loop = new TrackingLoop(
            _sampler,
            _sink,
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
        _sampler.IdleTime = TimeSpan.FromMinutes(2);

        // Act
        var changes = await StartAsync();

        // Assert
        Assert.Contains(new StateStarted(StartTime.AddMinutes(-2), ComputerState.Away), changes);
    }

    [Fact]
    public async Task StartingTheLoopStartsTheSink()
    {
        // Act
        await StartAsync();

        // Assert
        Assert.True(_sink.IsStarted);
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
        Assert.True(_sink.IsCompleted);
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

    private Task<IReadOnlyList<TrackingChange>> NextWriteAsync() => BackgroundWork.ReadNextAsync(_sink.Writes);

    private sealed class FakeSampler : ISystemSampler
    {
        public ForegroundWindow Window { get; set; } = Editor;

        public TimeSpan IdleTime { get; set; }

        public TrackingSample Read(DateTime timeUtc) => new(timeUtc, Window, IdleTime, IsLocked: false);
    }

    private sealed class FakeSink : ITrackingSink
    {
        public Channel<IReadOnlyList<TrackingChange>> Writes { get; } =
            Channel.CreateUnbounded<IReadOnlyList<TrackingChange>>();

        public bool IsStarted { get; private set; }

        public bool IsCompleted { get; private set; }

        public void Start() => IsStarted = true;

        public void Write(IReadOnlyList<TrackingChange> changes) => Writes.Writer.TryWrite(changes);

        public Task CompleteAsync()
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }
    }
}
