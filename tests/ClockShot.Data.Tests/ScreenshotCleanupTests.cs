using ClockShot.Core.Screenshots;
using ClockShot.Core.Settings;
using ClockShot.Data.Settings;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace ClockShot.Data.Tests;

public sealed class ScreenshotCleanupTests : IAsyncDisposable
{
    private static readonly DateTime Start = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly FakeTimeProvider _clock = new(Start);
    private readonly FakeScreenshotStore _screenshots = new();
    private readonly JsonSettingsStore _settings = MemorySettingsStore.Create();
    private readonly ScreenshotCleanup _cleanup;

    public ScreenshotCleanupTests()
    {
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
        _screenshots.DeleteFinishes = Task.FromException(new IOException("The disk is busy."));
        _cleanup.Start();
        await NextCutoffAsync();

        // Act
        _clock.Advance(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(Start.AddHours(1).AddDays(-7), await NextCutoffAsync());
    }

    private Task<DateTime> NextCutoffAsync() => BackgroundWork.ReadNextAsync(_screenshots.DeleteCutoffs);
}
