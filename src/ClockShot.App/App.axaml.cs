using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ClockShot.App.Views;

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
        var services = CompositionRoot.Build();
        desktop.MainWindow = MainWindow.Create(services);
    }
}
