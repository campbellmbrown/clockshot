namespace ClockShot.App.Services;

/// <summary>
/// Puts text on the clipboard.
/// </summary>
public interface IClipboardService
{
    Task SetTextAsync(string text);
}
