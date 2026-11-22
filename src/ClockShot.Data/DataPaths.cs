using ClockShot.Core;

namespace ClockShot.Data;

/// <summary>
/// Where the data files live.
/// Everything lives under one root folder.
/// </summary>
public sealed record DataPaths(string Root)
{
    public string Database => Path.Combine(Root, $"{AppInfo.FilePrefix}.db");
    public string Settings => Path.Combine(Root, "settings.json");
    public string Screenshots => Path.Combine(Root, "screenshots");
    public string Backups => Path.Combine(Root, "backups");
}
