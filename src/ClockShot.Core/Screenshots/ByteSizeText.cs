using System.Globalization;

namespace ClockShot.Core.Screenshots;

/// <summary>
/// Formats a number of bytes as KB, MB or GB, like "1.5 MB".
/// </summary>
public static class ByteSizeText
{
    private const long BytesPerKilobyte = 1024;
    private const long BytesPerMegabyte = 1024 * BytesPerKilobyte;
    private const long BytesPerGigabyte = 1024 * BytesPerMegabyte;

    public static string Format(long bytes) => bytes switch
    {
        >= BytesPerGigabyte => string.Create(CultureInfo.InvariantCulture, $"{bytes / (double)BytesPerGigabyte:0.0} GB"),
        >= BytesPerMegabyte => string.Create(CultureInfo.InvariantCulture, $"{bytes / (double)BytesPerMegabyte:0.0} MB"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{bytes / (double)BytesPerKilobyte:0} KB"),
    };
}
