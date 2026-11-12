namespace ClockShot.App;

/// <summary>
/// Command-line options. Normal use needs none of them.
/// </summary>
/// <param name="DataFolder">
/// The <c>--data-folder</c> option sets this folder.
/// The app keeps all its data here instead of in <see cref="AppPaths.Default"/>.
/// </param>
public sealed record StartupOptions(string? DataFolder)
{
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
                "--data-folder" => options with { DataFolder = Path.GetFullPath(TakeValue(unread, option)) },
                _ => throw new ArgumentException($"Unknown option {option}."),
            };
        }

        return options;
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
