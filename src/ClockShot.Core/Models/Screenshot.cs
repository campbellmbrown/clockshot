namespace ClockShot.Core.Models;

/// <summary>
/// Index entry for a screenshot file.
/// The path is relative to the screenshot store's root folder.
/// </summary>
public sealed record Screenshot(long Id, DateTime TakenUtc, string ImagePath, int Width, int Height);
