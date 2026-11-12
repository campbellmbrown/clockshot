using Avalonia;

namespace ClockShot.App;

internal static class Program
{
    /// <summary>
    /// The usual exit code for a command used the wrong way.
    /// </summary>
    private const int BadArgumentsExitCode = 2;

    [STAThread]
    public static int Main(string[] args)
    {
        StartupOptions options;
        try
        {
            options = StartupOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return BadArgumentsExitCode;
        }

        using var instance = new SingleInstance(options.Paths.Root);
        if (!instance.IsFirst)
        {
            return 0;
        }

        return BuildAvaloniaApp(options).StartWithClassicDesktopLifetime(args);
    }

    // Used by the XAML previewer.
    public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(StartupOptions.None);

    private static AppBuilder BuildAvaloniaApp(StartupOptions options) =>
        AppBuilder.Configure(() => new App(options))
            .UsePlatformDetect()
            .WithInterFont();
}
