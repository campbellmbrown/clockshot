using ClockShot.App.Services;

using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAppLifetime _lifetime;

    public MainWindowViewModel(IAppLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    [RelayCommand]
    private void Exit() => _lifetime.Shutdown();
}
