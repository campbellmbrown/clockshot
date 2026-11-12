using Avalonia.Logging;

using ClockShot.App.Logging;

using Microsoft.Extensions.Logging;

namespace ClockShot.App.Tests.Logging;

public sealed class AvaloniaLogSinkTests
{
    private readonly RecordingLogger<AvaloniaLogSinkTests> _logger = new();

    [Theory]
    [InlineData(LogEventLevel.Warning, LogLevel.Warning)]
    [InlineData(LogEventLevel.Error, LogLevel.Error)]
    [InlineData(LogEventLevel.Fatal, LogLevel.Critical)]
    public void PassesOnWarningsAndErrorsWithTheTemplateFilledIn(LogEventLevel level, LogLevel expected)
    {
        // Arrange
        var sink = new AvaloniaLogSink(_logger);

        // Act
        sink.Log(level, "Binding", null, "Error in binding to {Target}: {Message}", "Button", "not found");

        // Assert
        Assert.Equal((expected, "Binding: Error in binding to Button: not found", null), Assert.Single(_logger.Entries));
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose)]
    [InlineData(LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information)]
    public void LeavesOutMessagesBelowWarning(LogEventLevel level)
    {
        // Arrange
        var sink = new AvaloniaLogSink(_logger);

        // Act
        sink.Log(level, "Layout", null, "Measured {Control}", "Button");

        // Assert
        Assert.False(sink.IsEnabled(level, "Layout"));
        Assert.Empty(_logger.Entries);
    }
}
