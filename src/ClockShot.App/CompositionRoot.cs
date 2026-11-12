using ClockShot.App.Logging;
using ClockShot.App.Services;
using ClockShot.App.ViewModels;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace ClockShot.App;

/// <summary>
/// The one place where concrete types are chosen and wired together.
/// </summary>
internal static class CompositionRoot
{
    /// <param name="paths">Where the data and logs live.</param>
    public static ServiceProvider Build(AppPaths paths)
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton(paths);
        services.AddLogging(logging => logging.AddSerilog(LogFiles.Create(paths.Logs), dispose: true));

        // App services
        services.AddSingleton<IAppLifetime, DesktopAppLifetime>();
        services.AddSingleton<IShell, SystemShell>();
        services.AddSingleton<MainWindowHost>();
        services.AddSingleton<IDialogService, DialogService>();

        // View models
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TrayIconViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
