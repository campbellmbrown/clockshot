using System.IO.Abstractions;

using Avalonia.Media.Imaging;

using ClockShot.Core;
using ClockShot.Core.Models;
using ClockShot.Core.Screenshots;

namespace ClockShot.App.Services;

/// <summary>
/// Loads screenshot files into bitmaps on a background thread, and keeps the most recent ones in memory.
/// Moving back and forth along the timeline is then instant.
/// Screenshots up to <see cref="DecodeWidth"/> wide are loaded at full size, so they're sharp when zoomed in.
/// </summary>
public sealed class ScreenshotImageLoader(IScreenshotStore store, IFileSystem fileSystem)
{
    /// <summary>
    /// Wider screenshots are scaled down to this when loaded, to bound memory use.
    /// </summary>
    private const int DecodeWidth = 2560;

    /// <summary>
    /// Enough to move back and forth between nearby screenshots.
    /// A bitmap takes about 8 MB at 1920 x 1080 and about 15 MB at 2560 x 1440.
    /// Eight of the larger size come to about 120 MB.
    /// </summary>
    private const int CacheCapacity = 8;

    // Dropped bitmaps aren't disposed, because one may still be on screen.
    // The garbage collector frees them later.
    private readonly RecentlyUsedCache<long, Bitmap> _recentBitmaps = new(CacheCapacity);

    /// <summary>
    /// The bitmap for the screenshot, or null when its file can't be opened or decoded.
    /// </summary>
    public async Task<Bitmap?> LoadAsync(Screenshot screenshot, CancellationToken cancellationToken = default)
    {
        if (_recentBitmaps.Find(screenshot.Id) is { } cached)
        {
            return cached;
        }

        var bitmap = await Task.Run(() => Decode(screenshot), cancellationToken);
        if (bitmap is not null)
        {
            _recentBitmaps.Set(screenshot.Id, bitmap);
        }

        return bitmap;
    }

    private Bitmap? Decode(Screenshot screenshot)
    {
        try
        {
            using var stream = fileSystem.File.OpenRead(store.FullPath(screenshot.ImagePath));
            return screenshot.Width > DecodeWidth
                ? Bitmap.DecodeToWidth(stream, DecodeWidth, BitmapInterpolationMode.HighQuality)
                : new Bitmap(stream);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // The file was deleted, is locked, isn't readable, or isn't an image Avalonia can decode.
            return null;
        }
    }
}
