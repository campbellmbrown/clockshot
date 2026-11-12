using ClockShot.App.Services;
using ClockShot.App.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace ClockShot.App;

/// <summary>The one place where concrete types are chosen and wired together.</summary>
internal static class CompositionRoot
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        // App services
        services.AddSingleton<IAppLifetime, DesktopAppLifetime>();

        // View models
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
