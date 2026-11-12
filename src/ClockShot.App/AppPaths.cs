using ClockShot.Core;

namespace ClockShot.App;

/// <summary>
/// Where the app keeps its files. The logs sit next to the data.
/// </summary>
/// <param name="Root">The folder everything lives under. <see cref="StartupOptions"/> can change it.</param>
public sealed record AppPaths(string Root)
{
    /// <summary>
    /// The ClockShot folder in the user's local app data folder, which is %LOCALAPPDATA% on Windows.
    /// The <c>--data-folder</c> option replaces it.
    /// </summary>
    public static AppPaths Default { get; } =
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppInfo.Name));

    public string Logs => Path.Combine(Root, "logs");
}
