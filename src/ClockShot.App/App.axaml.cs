using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using ClockShot.App.Logging;
using ClockShot.App.Services;
using ClockShot.App.ViewModels;
using ClockShot.App.Views;
using ClockShot.Core.Screenshots;
using ClockShot.Core.Settings;
using ClockShot.Core.Tracking;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClockShot.App;

public sealed partial class App(StartupOptions options, SingleInstance? instance) : Application
{
    /// <summary>
    /// Avalonia's runtime XAML loader needs a public constructor without parameters.
    /// </summary>
    public App() : this(StartupOptions.None, null) { }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            StartDesktop(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void StartDesktop(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var services = CompositionRoot.Build(options.Paths);
        var logger = services.GetRequiredService<ILogger<App>>();
        LogUnhandledExceptions(logger);
        Avalonia.Logging.Logger.Sink = new AvaloniaLogSink(services.GetRequiredService<ILoggerFactory>().CreateLogger("Avalonia"));

        var window = MainWindow.Create(services);

        // A window that isn't the main window stays hidden until the tray icon shows it.
        var general = services.GetRequiredService<ISettingsStore>().Current.General;
        var startsInTray = options.StartedWithWindows && general.StartInTrayWithWindows;
        if (!startsInTray)
        {
            desktop.MainWindow = window;
        }

        desktop.Exit += (_, _) =>
            // Stops tracking and saves the open segments. Run off the UI thread so awaits inside can't deadlock on it.
            Task.Run(() => services.DisposeAsync().AsTask()).GetAwaiter().GetResult();

        services.GetRequiredService<TrackingLoop>().Start();
        services.GetRequiredService<ScreenshotCleanup>().Start();
        services.GetRequiredService<AwayPrompter>().Start();

        var tray = services.GetRequiredService<TrayIconViewModel>();
        tray.ShowWindowRequested += () => BringToFront(window);
        TrayIconView.Show(this, tray);

        // Launching ClockShot again while it runs shows this copy's window instead.
        instance?.ListenForLaunches(() => Dispatcher.UIThread.Post(() => BringToFront(window)));

        // The window can be hidden to the tray, so only an explicit Exit ends the app.
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    private static void LogUnhandledExceptions(ILogger<App> logger)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception");
        TaskScheduler.UnobservedTaskException += (_, e) => logger.LogError(e.Exception, "Something failed. The app carries on.");
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            // Log a failed command and keep running. Closing the app would also stop tracking.
            logger.LogError(e.Exception, "Something failed. The app carries on.");
            e.Handled = true;
        };
    }

    private static void BringToFront(Window window)
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }
}
