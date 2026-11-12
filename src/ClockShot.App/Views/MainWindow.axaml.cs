using Avalonia.Controls;

using ClockShot.App.Services;
using ClockShot.App.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace ClockShot.App.Views;

public sealed partial class MainWindow : Window
{
    /// <summary>
    /// The main window with its view model, set as the owner of dialogs.
    /// </summary>
    public static MainWindow Create(IServiceProvider services)
    {
        var window = new MainWindow { DataContext = services.GetRequiredService<MainWindowViewModel>() };
        services.GetRequiredService<MainWindowHost>().Window = window;
        return window;
    }

    public MainWindow()
    {
        InitializeComponent();
    }
}
