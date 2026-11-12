using System.Globalization;

using ClockShot.Core;

using Serilog;

namespace ClockShot.App.Logging;

/// <summary>
/// The app's log, one file per day in the logs folder.
/// </summary>
public static class LogFiles
{
    /// <summary>
    /// Two weeks of logs is long enough to look back at a problem.
    /// </summary>
    private const int KeepFiles = 14;

    // Level:u3 writes the level as three capital letters, such as WRN. Message:lj writes strings without quotes.
    private const string LineTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static Serilog.Core.Logger Create(string folder) =>
        new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(folder, $"{AppInfo.FilePrefix}-.log"),
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: LineTemplate,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: KeepFiles)
            .CreateLogger();
}
