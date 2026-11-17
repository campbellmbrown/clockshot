namespace ClockShot.Core.Timeline;

/// <summary>
/// A stored colour in "#RRGGBB" form, such as an application's.
/// </summary>
public sealed record HexColor(string Hex) : IBlockColor;
