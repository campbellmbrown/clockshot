using System.IO.Abstractions;

using ClockShot.App.Logging;
using ClockShot.App.Services;
using ClockShot.App.ViewModels;
using ClockShot.Core.Settings;
using ClockShot.Data.Settings;

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
        services.AddSingleton(paths.Data);
        services.AddLogging(logging => logging.AddSerilog(LogFiles.Create(paths.Logs), dispose: true));
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();

        // App services
        services.AddSingleton<IAppLifetime, DesktopAppLifetime>();
        services.AddSingleton<IShell, SystemShell>();
        services.AddSingleton<MainWindowHost>();
        services.AddSingleton<IDialogService, DialogService>();

        // View models
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TrayIconViewModel>();
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
