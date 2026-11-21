namespace ClockShot.Core.Timeline;

/// <summary>
/// A stored colour in "#RRGGBB" form, such as an application's or a tag's.
/// </summary>
public sealed record HexColor(string Hex) : IBlockColor;
