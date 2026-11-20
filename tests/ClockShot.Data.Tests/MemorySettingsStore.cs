using System.IO.Abstractions.TestingHelpers;

using ClockShot.Data.Settings;

using Microsoft.Extensions.Logging.Abstractions;

namespace ClockShot.Data.Tests;

/// <summary>
/// Builds the real settings store on a file system that lives in memory.
/// </summary>
internal static class MemorySettingsStore
{
    public static JsonSettingsStore Create() =>
        new(new MockFileSystem(), new DataPaths("data"), NullLogger<JsonSettingsStore>.Instance);
}
