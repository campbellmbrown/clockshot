using ClockShot.Core;

namespace ClockShot.App;

/// <summary>
/// Command-line options. Normal use needs none of them.
/// </summary>
/// <param name="DataFolder">
/// The <c>--data-folder</c> option sets this folder.
/// The app keeps all its data here instead of in <see cref="AppPaths.Default"/>.
/// </param>
/// <param name="StartedWithWindows">
/// The <c>--startup</c> flag sets this.
/// It means Windows started the app at sign-in because of the "Start with Windows" setting.
/// </param>
public sealed record StartupOptions(string? DataFolder, bool StartedWithWindows = false)
{
    public const string StartedWithWindowsFlag = "--startup";

    public const string DataFolderOption = "--data-folder";

    public static StartupOptions None { get; } = new(DataFolder: null);

    /// <summary>
    /// Where the data lives, which is <see cref="DataFolder"/> or the default folder.
    /// </summary>
    public AppPaths Paths => DataFolder is { } folder ? new AppPaths(folder) : AppPaths.Default;

    /// <exception cref="ArgumentException">An option is unknown or has no value.</exception>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        var options = None;
        var unread = new Queue<string>(args);
        while (unread.TryDequeue(out var option))
        {
            options = option switch
            {
                StartedWithWindowsFlag => options with { StartedWithWindows = true },
                DataFolderOption => options with { DataFolder = Path.GetFullPath(TakeValue(unread, option)) },
                _ => throw new ArgumentException($"Unknown option {option}."),
            };
        }

        return options;
    }

    /// <summary>
    /// The command Windows should run at sign-in, with <see cref="StartedWithWindowsFlag"/>.
    /// When VS Code runs the app through the dotnet host, the command runs it through dotnet too.
    /// It passes on a data folder other than the default, so a dev copy started at sign-in keeps its own data.
    /// </summary>
    /// <param name="processPath">The running executable, usually <see cref="Environment.ProcessPath"/>.</param>
    /// <param name="baseDirectory">The app's folder, usually <see cref="AppContext.BaseDirectory"/>.</param>
    /// <param name="paths">Where this copy keeps its data.</param>
    public static string StartWithWindowsCommand(string? processPath, string baseDirectory, AppPaths paths)
    {
        var process = processPath ?? Path.Combine(baseDirectory, $"{AppInfo.Name}.exe");
        var isDotnetHost = Path.GetFileNameWithoutExtension(process).Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        var app = isDotnetHost ? $"\"{process}\" \"{Path.Combine(baseDirectory, $"{AppInfo.Name}.dll")}\"" : $"\"{process}\"";

        // Windows reads a backslash before a quote as an escaped quote, so a trailing one is doubled.
        var folder = paths.Root.EndsWith('\\') ? paths.Root + '\\' : paths.Root;
        var dataFolder = paths == AppPaths.Default ? "" : $" {DataFolderOption} \"{folder}\"";

        return $"{app}{dataFolder} {StartedWithWindowsFlag}";
    }

    /// <summary>
    /// Takes the value that follows an option.
    /// </summary>
    /// <exception cref="ArgumentException">Nothing follows the option.</exception>
    private static string TakeValue(Queue<string> unread, string option)
    {
        if (!unread.TryDequeue(out var value))
        {
            throw new ArgumentException($"{option} needs a value.");
        }

        return value;
    }
}
