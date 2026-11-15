using System.Collections.ObjectModel;

using ClockShot.App.Controls;
using ClockShot.Core.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        ExcludedApps = [.. settings.Current.Screenshots.ExcludedProcessNames];

        // Saving doesn't notify the bindings, so the page refreshes after every change.
        // That updates rows that depend on another setting, and shows changes made in other places.
        settings.Changed += _ => OnUiThread(() => OnPropertyChanged(string.Empty));
    }

    private AppSettings Current => _settings.Current;

    private void Update(Func<AppSettings, AppSettings> change) => _settings.Save(change(_settings.Current));

    /// <summary>
    /// Saves the value of a number box.
    /// A cleared box changes nothing and shows the saved value again.
    /// </summary>
    private void UpdateNumber(decimal? value, Func<AppSettings, int, AppSettings> change)
    {
        if (value is { } number)
        {
            Update(s => change(s, (int)number));
        }
        else
        {
            OnPropertyChanged(string.Empty);
        }
    }

    // ---- General ----

    public bool CloseToTray
    {
        get => Current.General.CloseToTray;
        set => Update(s => s with { General = s.General with { CloseToTray = value } });
    }

    // ---- Tracking ----

    public decimal? AwayAfterMinutes
    {
        get => Current.Tracking.AwayAfterMinutes;
        set => UpdateNumber(value, (s, v) => s with { Tracking = s.Tracking with { AwayAfterMinutes = v } });
    }

    // ---- Screenshots ----

    public bool ScreenshotsEnabled
    {
        get => Current.Screenshots.Enabled;
        set => Update(s => s with { Screenshots = s.Screenshots with { Enabled = value } });
    }

    public decimal? ScreenshotIntervalSeconds
    {
        get => Current.Screenshots.IntervalSeconds;
        set => UpdateNumber(value, (s, v) => s with { Screenshots = s.Screenshots with { IntervalSeconds = v } });
    }

    public bool CaptureOnAppSwitch
    {
        get => Current.Screenshots.CaptureOnAppSwitch;
        set => Update(s => s with { Screenshots = s.Screenshots with { CaptureOnAppSwitch = value } });
    }

    public decimal? ScreenshotMaxWidthPixels
    {
        get => Current.Screenshots.MaxWidthPixels;
        set => UpdateNumber(value, (s, v) => s with { Screenshots = s.Screenshots with { MaxWidthPixels = v } });
    }

    public decimal? ScreenshotQuality
    {
        get => Current.Screenshots.Quality;
        set => UpdateNumber(value, (s, v) => s with { Screenshots = s.Screenshots with { Quality = v } });
    }

    public decimal? KeepScreenshotsForDays
    {
        get => Current.Screenshots.KeepForDays;
        set => UpdateNumber(value, (s, v) => s with { Screenshots = s.Screenshots with { KeepForDays = v } });
    }

    /// <summary>
    /// The process names of apps that are never captured.
    /// </summary>
    public ObservableCollection<string> ExcludedApps { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddExcludedAppCommand))]
    public partial string? NewExcludedApp { get; set; }

    [RelayCommand(CanExecute = nameof(CanAddExcludedApp))]
    private void AddExcludedApp()
    {
        ExcludedApps.Add(NewExcludedApp!.Trim());
        NewExcludedApp = null;
        SaveExcludedApps();
    }

    private bool CanAddExcludedApp() =>
        !string.IsNullOrWhiteSpace(NewExcludedApp)
        && !ExcludedApps.Contains(NewExcludedApp.Trim(), StringComparer.OrdinalIgnoreCase);

    [RelayCommand]
    private void RemoveExcludedApp(string name)
    {
        ExcludedApps.Remove(name);
        SaveExcludedApps();
    }

    private void SaveExcludedApps() =>
        Update(s => s with { Screenshots = s.Screenshots with { ExcludedProcessNames = [.. ExcludedApps] } });
}
