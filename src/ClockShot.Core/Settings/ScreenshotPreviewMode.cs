using System.Text.Json.Serialization;

namespace ClockShot.Core.Settings;

/// <summary>
/// Where the screenshot preview appears.
/// Each value is saved by the name it's pinned to, so renaming a value doesn't change the settings file.
/// </summary>
public enum ScreenshotPreviewMode
{
    /// <summary>
    /// A popup beside the mouse pointer.
    /// </summary>
    [JsonStringEnumMemberName("PopupByCursor")]
    PopupByCursor,

    /// <summary>
    /// The space below the timeline.
    /// </summary>
    [JsonStringEnumMemberName("BelowTimeline")]
    BelowTimeline,

    /// <summary>
    /// A separate window that can be moved, even to another monitor.
    /// </summary>
    [JsonStringEnumMemberName("SeparateWindow")]
    SeparateWindow,
}
