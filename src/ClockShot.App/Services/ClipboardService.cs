using Avalonia.Input.Platform;

namespace ClockShot.App.Services;

internal sealed class ClipboardService(MainWindowHost host) : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        if (host.Window.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}
