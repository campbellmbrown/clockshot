using Avalonia.Media.Imaging;

using ClockShot.App.Controls;
using ClockShot.App.Services;
using ClockShot.Core.Models;
using ClockShot.Core.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// The screenshot shown while hovering the timeline, where it's shown, and whether it's paused on one screenshot.
/// The same view model feeds all three places the preview can be shown.
/// </summary>
public sealed partial class ScreenshotPreviewViewModel : ViewModelBase, IDisposable
{
    private readonly ScreenshotImageLoader _images;
    private readonly ISettingsStore _settings;
    private readonly NewestLoad _newestLoad = new();

    public ScreenshotPreviewViewModel(ScreenshotImageLoader images, ISettingsStore settings)
    {
        _images = images;
        _settings = settings;

        // The mode can also change on the Settings tab.
        _settings.Changed += OnSettingsChanged;
    }

    /// <summary>
    /// Raised when the preview is paused or resumed, since the timeline marks where it's paused.
    /// </summary>
    public event Action? PauseChanged;

    /// <summary>
    /// The screenshot being shown, or null.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    public partial Bitmap? Image { get; private set; }

    public bool HasImage => Image is not null;

    /// <summary>
    /// When the screenshot was taken, in local time.
    /// </summary>
    [ObservableProperty]
    public partial string? TakenText { get; private set; }

    [ObservableProperty]
    public partial string? ApplicationName { get; private set; }

    [ObservableProperty]
    public partial string? WindowTitle { get; private set; }

    /// <summary>
    /// Shown instead of an image, to say why there's no screenshot.
    /// </summary>
    [ObservableProperty]
    public partial string Message { get; private set; } = "Hover over the timeline to see what was on screen.";

    /// <summary>
    /// True while the pointer is over the timeline.
    /// The popup only shows while hovering.
    /// </summary>
    [ObservableProperty]
    public partial bool IsHovering { get; set; }

    // ---- Where the preview is shown ----

    public ScreenshotPreviewMode Mode
    {
        get => _settings.Current.Screenshots.PreviewMode;
        set
        {
            if (value == Mode)
            {
                return;
            }

            var current = _settings.Current;
            _settings.Save(current with { Screenshots = current.Screenshots with { PreviewMode = value } });
            NotifyModeChanged();
        }
    }

    /// <summary>
    /// The modes offered in the timeline toolbar's dropdown.
    /// </summary>
    public IReadOnlyList<PreviewModeChoice> Choices { get; } = PreviewModeChoice.All;

    /// <summary>
    /// The current mode, as chosen in the timeline toolbar's dropdown.
    /// </summary>
    public PreviewModeChoice Choice
    {
        get => PreviewModeChoice.For(Mode);
        set
        {
            // A dropdown can briefly pass null while it rebuilds.
            if (value is not null)
            {
                Mode = value.Mode;
            }
        }
    }

    public bool IsPopup => Mode == ScreenshotPreviewMode.PopupByCursor;

    public bool IsBelowTimeline => Mode == ScreenshotPreviewMode.BelowTimeline;

    public bool IsInWindow => Mode == ScreenshotPreviewMode.SeparateWindow;

    // ---- Pausing on one screenshot ----

    /// <summary>
    /// The time the preview last showed, so pausing knows where to stay.
    /// </summary>
    public DateTime? ShownAtUtc { get; set; }

    /// <summary>
    /// Where the preview is paused, or null when it follows the pointer.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPaused), nameof(PauseText), nameof(PauseIconKind))]
    public partial DateTime? PausedAtUtc { get; private set; }

    public bool IsPaused => PausedAtUtc is not null;

    public string PauseText => IsPaused ? "Resume the preview" : "Pause the preview here";

    public string PauseIconKind => IsPaused ? Icons.ResumePreview : Icons.PausePreview;

    /// <summary>
    /// Shows a screenshot, loading it in the background.
    /// Its time, app and window title change with the image, so they never describe the previous screenshot.
    /// A newer call cancels an unfinished older one.
    /// </summary>
    public async Task ShowAsync(Screenshot screenshot, string takenText, string applicationName, string windowTitle)
    {
        var token = _newestLoad.Start();
        try
        {
            var image = await _images.LoadAsync(screenshot, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            TakenText = takenText;
            ApplicationName = applicationName;
            WindowTitle = windowTitle;
            Image = image;
            Message = image is null ? "This screenshot couldn't be opened." : "";
        }
        catch (OperationCanceledException)
        {
            // A newer hover took over.
        }
    }

    /// <summary>
    /// Says why there's no screenshot to show at this spot.
    /// </summary>
    public void ShowMessage(string message, string? applicationName = null, string? windowTitle = null)
    {
        _newestLoad.Cancel();
        Image = null;
        TakenText = null;
        ApplicationName = applicationName;
        WindowTitle = windowTitle;
        Message = message;
    }

    public void Resume() => PausedAtUtc = null;

    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        _newestLoad.Dispose();
    }

    [RelayCommand]
    private void SetMode(ScreenshotPreviewMode mode) => Mode = mode;

    /// <summary>
    /// Moves to the next mode in <see cref="PreviewModeChoice.All"/>, wrapping round to the first.
    /// </summary>
    [RelayCommand]
    private void CycleMode() => Mode = Choice.Next().Mode;

    /// <summary>
    /// Pauses the preview on what it's showing now, so the pointer can move away, or follows the pointer again.
    /// </summary>
    [RelayCommand]
    private void TogglePause() => PausedAtUtc = IsPaused ? null : ShownAtUtc;

    partial void OnPausedAtUtcChanged(DateTime? value) => PauseChanged?.Invoke();

    private void OnSettingsChanged(AppSettings settings) => OnUiThread(NotifyModeChanged);

    private void NotifyModeChanged()
    {
        OnPropertyChanged(nameof(Mode));
        OnPropertyChanged(nameof(Choice));
        OnPropertyChanged(nameof(IsPopup));
        OnPropertyChanged(nameof(IsBelowTimeline));
        OnPropertyChanged(nameof(IsInWindow));
    }
}
