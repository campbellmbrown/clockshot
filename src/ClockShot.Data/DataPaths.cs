namespace ClockShot.Data;

/// <summary>
/// Where the data files live.
/// Everything lives under one root folder.
/// </summary>
public sealed record DataPaths(string Root)
{
    public string Settings => Path.Combine(Root, "settings.json");
}
