using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using ClockShot.App.Logging;
using ClockShot.App.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClockShot.App;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            StartDesktop(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void StartDesktop(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var services = CompositionRoot.Build(AppPaths.Default);
        var logger = services.GetRequiredService<ILogger<App>>();
        LogUnhandledExceptions(logger);
        Avalonia.Logging.Logger.Sink = new AvaloniaLogSink(services.GetRequiredService<ILoggerFactory>().CreateLogger("Avalonia"));

        desktop.MainWindow = MainWindow.Create(services);
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
}
