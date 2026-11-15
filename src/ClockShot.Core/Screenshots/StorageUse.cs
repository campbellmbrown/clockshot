using System.Globalization;

namespace ClockShot.Core.Screenshots;

/// <summary>
/// How much space screenshots take.
/// </summary>
public sealed record StorageUse(long Bytes, int Files)
{
    /// <summary>
    /// The size and count as the Settings page shows them.
    /// </summary>
    public string Text => string.Create(
        CultureInfo.InvariantCulture,
        $"{ByteSizeText.Format(Bytes)} in {Files:N0} {(Files == 1 ? "screenshot" : "screenshots")}");
}
