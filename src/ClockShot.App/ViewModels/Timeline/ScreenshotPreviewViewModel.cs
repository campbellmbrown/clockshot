using Avalonia.Media.Imaging;

using ClockShot.App.Services;
using ClockShot.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// The screenshot shown while hovering the timeline.
/// </summary>
public sealed partial class ScreenshotPreviewViewModel : ViewModelBase, IDisposable
{
    private readonly ScreenshotImageLoader _images;
    private CancellationTokenSource? _loading;

    public ScreenshotPreviewViewModel(ScreenshotImageLoader images)
    {
        _images = images;
    }

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

    /// <summary>
    /// Shows a screenshot, loading it in the background.
    /// Its time, app and window title change with the image, so they never describe the previous screenshot.
    /// A newer call cancels an unfinished older one.
    /// </summary>
    public async Task ShowAsync(Screenshot screenshot, string takenText, string applicationName, string windowTitle)
    {
        var loading = Restart();
        try
        {
            var image = await _images.LoadAsync(screenshot, loading.Token);
            if (loading.IsCancellationRequested)
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
        Restart();
        Image = null;
        TakenText = null;
        ApplicationName = applicationName;
        WindowTitle = windowTitle;
        Message = message;
    }

    public void Dispose()
    {
        _loading?.Cancel();
        _loading?.Dispose();
    }

    /// <summary>
    /// Cancels the previous load and starts a new one.
    /// The old source is only cancelled, not disposed, because its load may still be checking it.
    /// </summary>
    private CancellationTokenSource Restart()
    {
        _loading?.Cancel();
        return _loading = new CancellationTokenSource();
    }
}
