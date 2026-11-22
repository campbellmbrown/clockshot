using System.Collections.ObjectModel;

using ClockShot.App.Controls;
using ClockShot.App.Services;
using ClockShot.App.ViewModels.Timeline;
using ClockShot.App.ViewModels.Timesheets;
using ClockShot.Core.Screenshots;
using ClockShot.Core.Settings;
using ClockShot.Core.Timeline;
using ClockShot.Data;

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
    private readonly IScreenshotStore _screenshots;
    private readonly IShell _shell;
    private readonly IActivityHistory _history;
    private readonly DataPaths _paths;
    private readonly BackgroundWork _background;

    public SettingsViewModel(
        ISettingsStore settings,
        IScreenshotStore screenshots,
        IShell shell,
        IActivityHistory history,
        DataPaths paths,
        BackgroundWork background)
        : base("Settings", Icons.Settings)
    {
        _settings = settings;
        _screenshots = screenshots;
        _shell = shell;
        _history = history;
        _paths = paths;
        _background = background;
        ExcludedApps = [.. settings.Current.Screenshots.ExcludedProcessNames];

        // Saving doesn't notify the bindings, so the page refreshes after every change.
        // That updates rows that depend on another setting, and shows changes made in other places.
        settings.Changed += _ => OnUiThread(() => OnPropertyChanged(string.Empty));
    }

    /// <summary>
    /// Counts the space the screenshots use and loads the app suggestions, because both change while the app runs.
    /// </summary>
    public override void OnShown()
    {
        _background.Run(CountStorageAsync);
        _background.Run(LoadKnownAppsAsync);
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

    public decimal? DayStartHour
    {
        get => Current.General.DayStartHour;
        set => UpdateNumber(value, (s, v) => s with { General = s.General with { DayStartHour = v } });
    }

    // ---- Tracking ----

    public decimal? AwayAfterMinutes
    {
        get => Current.Tracking.AwayAfterMinutes;
        set => UpdateNumber(value, (s, v) => s with { Tracking = s.Tracking with { AwayAfterMinutes = v } });
    }

    public bool AwayPromptEnabled
    {
        get => Current.Tracking.AwayPromptEnabled;
        set => Update(s => s with { Tracking = s.Tracking with { AwayPromptEnabled = value } });
    }

    public decimal? AwayPromptAfterMinutes
    {
        get => Current.Tracking.AwayPromptAfterMinutes;
        set => UpdateNumber(value, (s, v) => s with { Tracking = s.Tracking with { AwayPromptAfterMinutes = v } });
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

    public IReadOnlyList<PreviewModeChoice> PreviewModes { get; } = PreviewModeChoice.All;

    public PreviewModeChoice PreviewMode
    {
        get => PreviewModeChoice.For(Current.Screenshots.PreviewMode);
        set => Update(s => s with { Screenshots = s.Screenshots with { PreviewMode = value.Mode } });
    }

    /// <summary>
    /// The process names of apps that are never captured.
    /// </summary>
    public ObservableCollection<string> ExcludedApps { get; }

    /// <summary>
    /// The process names of apps seen so far, suggested for <see cref="NewExcludedApp"/>.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> KnownApps { get; private set; } = [];

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

    /// <summary>
    /// How much space the screenshots use, and how many there are.
    /// It's counted again each time the tab is shown.
    /// Screenshots are saved and deleted all the time.
    /// </summary>
    [ObservableProperty]
    public partial string StorageText { get; private set; } = "";

    [RelayCommand]
    private void OpenScreenshotFolder() => _shell.OpenFolder(_paths.Screenshots);

    // ---- Timesheet ----

    public IReadOnlyList<RoundingChoice> RoundingChoices { get; } = RoundingChoice.All;

    public RoundingChoice Rounding
    {
        get => RoundingChoice.For(Current.Timesheet);
        set => Update(s => s with { Timesheet = value.ApplyTo(s.Timesheet) });
    }

    // ---- Loading ----

    private async Task LoadKnownAppsAsync()
    {
        var apps = await _history.GetApplicationsAsync();
        var processNames = apps.Select(a => a.ProcessName).Distinct(StringComparer.OrdinalIgnoreCase);
        KnownApps = [.. processNames.Order(StringComparer.OrdinalIgnoreCase)];
    }

    private async Task CountStorageAsync()
    {
        var storage = await _screenshots.GetStorageUseAsync();
        StorageText = storage.Text;
    }
}
