using ClockShot.App.Services;

using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAppLifetime _lifetime;
    private readonly IShell _shell;
    private readonly AppPaths _paths;

    public MainWindowViewModel(
        IAppLifetime lifetime,
        IShell shell,
        AppPaths paths)
    {
        _lifetime = lifetime;
        _shell = shell;
        _paths = paths;
    }

    [RelayCommand]
    private void OpenLogFolder() => _shell.OpenFolder(_paths.Logs);

    [RelayCommand]
    private void Exit() => _lifetime.Shutdown();
}
