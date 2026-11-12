using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace ClockShot.App.Services;

internal sealed class DesktopAppLifetime : IAppLifetime
{
    public void Shutdown()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
