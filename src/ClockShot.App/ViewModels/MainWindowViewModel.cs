using ClockShot.App.Services;
using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Settings;
using ClockShot.Core.Tagging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAppLifetime _lifetime;
    private readonly IShell _shell;
    private readonly IDialogService _dialogs;
    private readonly ISettingsStore _settings;
    private readonly AppPaths _paths;
    private readonly Tagger _tagger;

    public MainWindowViewModel(
        TimelineViewModel timeline,
        SettingsViewModel settingsPage,
        TrackingStatusViewModel tracking,
        StatusBarViewModel statusBar,
        IAppLifetime lifetime,
        IShell shell,
        IDialogService dialogs,
        ISettingsStore settings,
        AppPaths paths,
        Tagger tagger)
    {
        Timeline = timeline;
        Settings = settingsPage;
        Tracking = tracking;
        StatusBar = statusBar;
        Pages = [timeline, settingsPage];
        SelectedPage = timeline;
        _lifetime = lifetime;
        _shell = shell;
        _dialogs = dialogs;
        _settings = settings;
        _paths = paths;
        _tagger = tagger;
        OnTimeline = new ShowPageFirstCommand(() => SelectedPage = Timeline);
        tagger.Changed += () => OnUiThread(OnTagsChanged);
    }

    public TimelineViewModel Timeline { get; }
    public SettingsViewModel Settings { get; }
    public TrackingStatusViewModel Tracking { get; }
    public StatusBarViewModel StatusBar { get; }

    /// <summary>
    /// The tabs, in display order.
    /// </summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    [ObservableProperty]
    public partial PageViewModel SelectedPage { get; set; }

    /// <summary>
    /// Whether closing the window hides it to the tray instead of exiting, so tracking keeps running.
    /// </summary>
    public bool CloseToTray => _settings.Current.General.CloseToTray;

    /// <summary>
    /// Runs the Timeline command given as the parameter, showing the Timeline tab first.
    /// </summary>
    public ShowPageFirstCommand OnTimeline { get; }

    /// <summary>
    /// The text of the Edit > Undo menu item, which names the change it would undo.
    /// Tagging happens on several tabs, so undo lives here.
    /// </summary>
    public string UndoText => _tagger.UndoDescription ?? "Undo last change";

    partial void OnSelectedPageChanged(PageViewModel value) => value.OnShown();

    [RelayCommand]
    private void ShowPage(PageViewModel page) => SelectedPage = page;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private Task UndoAsync() => _tagger.UndoAsync();

    private bool CanUndo() => _tagger.CanUndo;

    private void OnTagsChanged()
    {
        OnPropertyChanged(nameof(UndoText));
        UndoCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void OpenLogFolder() => _shell.OpenFolder(_paths.Logs);

    [RelayCommand]
    private Task ShowShortcuts() => _dialogs.ShowAsync(new KeyboardShortcutsViewModel());

    [RelayCommand]
    private Task ShowAbout() => _dialogs.ShowAsync(new AboutViewModel());

    [RelayCommand]
    private void Exit() => _lifetime.Shutdown();
}
