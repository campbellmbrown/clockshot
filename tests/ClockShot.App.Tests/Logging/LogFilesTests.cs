using ClockShot.App.Logging;
using ClockShot.Core;

using Microsoft.Extensions.Logging;

using Serilog.Extensions.Logging;

namespace ClockShot.App.Tests.Logging;

public sealed class LogFilesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"{AppInfo.FilePrefix}-tests", Guid.NewGuid().ToString("N"));

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void WritesEachLineToADatedFileWithItsCategory()
    {
        // Act
        using (var loggers = new SerilogLoggerFactory(LogFiles.Create(_folder), dispose: true))
        {
            loggers.CreateLogger("Test").LogWarning("Hello");
        }

        // Assert
        var file = Assert.Single(Directory.GetFiles(_folder));
        Assert.Matches($@"^{AppInfo.FilePrefix}-\d{{8}}\.log$", Path.GetFileName(file));
        Assert.Contains("[WRN] Test: Hello", File.ReadAllText(file));
    }
}
