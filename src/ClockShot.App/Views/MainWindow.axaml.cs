using Avalonia.Controls;

using ClockShot.App.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace ClockShot.App.Views;

public sealed partial class MainWindow : Window
{
    /// <summary>
    /// The main window with its view model.
    /// </summary>
    public static MainWindow Create(IServiceProvider services) =>
        new() { DataContext = services.GetRequiredService<MainWindowViewModel>() };

    public MainWindow()
    {
        InitializeComponent();
    }
}
