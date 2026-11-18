using ClockShot.App.Controls;
using ClockShot.Core.Settings;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// A screenshot preview mode as people see it, with its label and icon.
/// Settings, the View menu, the timeline toolbar and Ctrl+P all use these, so they're defined once here.
/// </summary>
public sealed record PreviewModeChoice(ScreenshotPreviewMode Mode, string Label, string Icon)
{
    public static PreviewModeChoice BesidePointer { get; } = new(ScreenshotPreviewMode.PopupByCursor, "Beside the pointer", Icons.PreviewBesidePointer);
    public static PreviewModeChoice BelowTimeline { get; } = new(ScreenshotPreviewMode.BelowTimeline, "Below the timeline", Icons.PreviewBelowTimeline);
    public static PreviewModeChoice InWindow { get; } = new(ScreenshotPreviewMode.SeparateWindow, "In a separate window", Icons.PreviewInWindow);

    public static IReadOnlyList<PreviewModeChoice> All { get; } = [BesidePointer, BelowTimeline, InWindow];

    public static PreviewModeChoice For(ScreenshotPreviewMode mode) => All.FirstOrDefault(c => c.Mode == mode) ?? BesidePointer;

    /// <summary>
    /// The next mode, wrapping round to the first.
    /// It's a method, not a property, so a record's ToString doesn't follow it round forever.
    /// </summary>
    public PreviewModeChoice Next() => All[(All.ToList().IndexOf(this) + 1) % All.Count];
}
