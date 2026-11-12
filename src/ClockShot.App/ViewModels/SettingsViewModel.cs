using ClockShot.App.Controls;
using ClockShot.Core.Settings;

namespace ClockShot.App.ViewModels;

/// <summary>
/// The Settings tab.
/// Every change is saved straight away, so there's no Save button.
/// Changes take effect at once, because running services read the current settings and other tabs listen for changes.
/// </summary>
public sealed partial class SettingsViewModel : PageViewModel
{
    private readonly ISettingsStore _settings;

    public SettingsViewModel(ISettingsStore settings)
        : base("Settings", Icons.Settings)
    {
        _settings = settings;
    }

    private AppSettings Current => _settings.Current;

    private void Update(Func<AppSettings, AppSettings> change) => _settings.Save(change(_settings.Current));

    // ---- General ----

    public bool CloseToTray
    {
        get => Current.General.CloseToTray;
        set => Update(s => s with { General = s.General with { CloseToTray = value } });
    }
}
