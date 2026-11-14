using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;
using ClockShot.Core.Settings;
using ClockShot.Core.Tracking;

namespace ClockShot.Core.Tests.Screenshots;

public sealed class ScreenshotSchedulerTests
{
    private static readonly DateTime Start = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ForegroundWindow Editor = new("Editor.exe", null, "Editor", "a.cs");
    private static readonly ScreenshotSettings Defaults = new() { IntervalSeconds = 30 };

    private readonly ScreenshotScheduler _scheduler = new();

    [Fact]
    public void CapturesImmediatelyThenEveryInterval()
    {
        // Act
        var captured = Run(from: 0, until: 70, _ => Editor);

        // Assert
        Assert.Equal([0, 30, 60], captured);
    }

    [Fact]
    public void CapturesAtOnceWhenTheClockGoesBackThenEveryInterval()
    {
        // Arrange
        _scheduler.MarkCaptured(Start);

        // Act
        var captured = Run(from: -3600, until: -3540, _ => Editor);

        // Assert
        Assert.Equal([-3600, -3570, -3540], captured);
    }

    [Theory]
    [InlineData(ComputerState.Away)]
    [InlineData(ComputerState.Locked)]
    [InlineData(null)]
    public void NeverCapturesWhenNotActive(ComputerState? state)
    {
        // Act
        var captures = Check(seconds: 0, Editor, state: state);

        // Assert
        Assert.False(captures);
    }

    [Fact]
    public void NeverCapturesWhenDisabled()
    {
        // Act
        var captures = Check(seconds: 0, Editor, Defaults with { Enabled = false });

        // Assert
        Assert.False(captures);
    }

    [Fact]
    public void NeverCapturesWithoutWindow()
    {
        // Act
        var captures = Check(seconds: 0, window: null);

        // Assert
        Assert.False(captures);
    }

    private bool Check(
        int seconds,
        ForegroundWindow? window,
        ScreenshotSettings? settings = null,
        ComputerState? state = ComputerState.Active)
    {
        var sample = new TrackingSample(Start.AddSeconds(seconds), window, IdleTime: TimeSpan.Zero, IsLocked: false);
        return _scheduler.ShouldCapture(sample, state, settings ?? Defaults);
    }

    /// <summary>
    /// Takes a sample every second from <paramref name="from"/> to <paramref name="until"/>, both included.
    /// Returns the seconds when a screenshot was taken.
    /// </summary>
    private List<int> Run(int from, int until, Func<int, ForegroundWindow?> windowAt)
    {
        var captured = new List<int>();
        for (var second = from; second <= until; second++)
        {
            if (!Check(second, windowAt(second)))
            {
                continue;
            }

            _scheduler.MarkCaptured(Start.AddSeconds(second));
            captured.Add(second);
        }

        return captured;
    }
}
