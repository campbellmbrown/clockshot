using System.Globalization;
using System.Text.RegularExpressions;

using Avalonia.Logging;

using Microsoft.Extensions.Logging;

namespace ClockShot.App.Logging;

/// <summary>
/// Sends Avalonia's own warnings and errors, such as binding failures, to the app's log.
/// </summary>
public sealed partial class AvaloniaLogSink(ILogger logger) : ILogSink
{
    /// <summary>
    /// Lower levels are left out because they would flood the log.
    /// </summary>
    private const LogEventLevel MinimumLevel = LogEventLevel.Warning;

    public bool IsEnabled(LogEventLevel level, string area) =>
        level >= MinimumLevel && logger.IsEnabled(ToLogLevel(level));

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Log(level, area, source, messageTemplate, []);

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if (!IsEnabled(level, area))
        {
            return;
        }

        var logLevel = ToLogLevel(level);
        var message = FillPlaceholders(messageTemplate, propertyValues);
        logger.Log(logLevel, "{Area}: {Message}", area, message);
    }

    /// <summary>
    /// Replaces each <c>{Name}</c> placeholder in Avalonia's message with the next value.
    /// </summary>
    private static string FillPlaceholders(string template, object?[] values)
    {
        var next = 0;
        return Placeholder().Replace(template, placeholder =>
            next < values.Length ? Convert.ToString(values[next++], CultureInfo.InvariantCulture) ?? "null" : placeholder.Value);
    }

    private static LogLevel ToLogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        LogEventLevel.Fatal => LogLevel.Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Only warnings and errors are logged."),
    };

    [GeneratedRegex(@"\{[^{}]+\}")]
    private static partial Regex Placeholder();
}
