using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Settings;

namespace ClockShot.App.Views.Timeline;

/// <summary>
/// The Timeline tab's behaviour that only the view needs.
/// It keeps the preview popup beside the pointer.
/// It also opens and closes the separate preview window.
/// </summary>
public sealed partial class TimelineView : UserControl
{
    /// <summary>
    /// The popup preview can use up to this share of the window's width and height.
    /// </summary>
    private const double PopupShareOfWindow = 0.6;
    private const double PopupPointerGap = 16;

    private TimelineViewModel? _timeline;
    private ScreenshotPreviewWindow? _previewWindow;

    public TimelineView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _timeline?.Preview.PropertyChanged -= OnPreviewChanged;
        _timeline = DataContext as TimelineViewModel;
        _timeline?.Preview.PropertyChanged += OnPreviewChanged;
        UpdatePreviewWindow();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdatePreviewWindow();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ClosePreviewWindow();
    }

    private void OnPreviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScreenshotPreviewViewModel.Mode))
        {
            UpdatePreviewWindow();
        }
    }

    // ---- Preview popup and window ----

    /// <summary>
    /// Keeps the popup beside the pointer, without running off the right edge of the window.
    /// </summary>
    private void OnTimelinePointerMoved(object? sender, PointerEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not { } window)
        {
            return;
        }

        PopupPreview.MaxWidth = window.Bounds.Width * PopupShareOfWindow;
        PopupPreview.MaxHeight = window.Bounds.Height * PopupShareOfWindow;

        var x = e.GetPosition(TimelineArea).X;

        // Before its first layout the popup has no width, so it's taken to be as wide as the preview can grow.
        var laidOutWidth = PreviewPopup.Child?.Bounds.Width ?? 0;
        var popupWidth = laidOutWidth > 0 ? laidOutWidth : PopupPreview.MaxWidth;
        var rightmostOffset = Math.Max(0, TimelineArea.Bounds.Width - popupWidth);
        PreviewPopup.HorizontalOffset = Math.Clamp(x + PopupPointerGap, min: 0, max: rightmostOffset);
    }

    private void UpdatePreviewWindow()
    {
        if (_timeline is { Preview: { IsInWindow: true } preview } && TopLevel.GetTopLevel(this) is not null)
        {
            if (_previewWindow is null)
            {
                OpenPreviewWindow(preview);
            }
        }
        else
        {
            ClosePreviewWindow();
        }
    }

    private void OpenPreviewWindow(ScreenshotPreviewViewModel preview)
    {
        _previewWindow = new ScreenshotPreviewWindow { DataContext = preview };
        _previewWindow.Closing += OnPreviewWindowClosing;
        _previewWindow.Closed += OnPreviewWindowClosed;
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            _previewWindow.Show(owner);
        }
        else
        {
            _previewWindow.Show();
        }
    }

    private void ClosePreviewWindow()
    {
        if (_previewWindow is null)
        {
            return;
        }

        _previewWindow.Closing -= OnPreviewWindowClosing;
        _previewWindow.Closed -= OnPreviewWindowClosed;
        _previewWindow.Close();
        _previewWindow = null;
    }

    /// <summary>
    /// Closing the preview window yourself switches back to the popup preview.
    /// Exiting the app closes it with another reason, which keeps the mode for next time.
    /// </summary>
    private void OnPreviewWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.WindowClosing)
        {
            _previewWindow = null;
            _timeline?.Preview.Mode = ScreenshotPreviewMode.PopupByCursor;
        }
    }

    private void OnPreviewWindowClosed(object? sender, EventArgs e) => _previewWindow = null;
}
