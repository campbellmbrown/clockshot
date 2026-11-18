namespace ClockShot.Core.Timeline;

/// <summary>
/// What the details panel says a selected block is.
/// </summary>
/// <param name="Heading">The application or computer state.</param>
/// <param name="Subheading">More detail, such as the window title, or empty when there's nothing to add.</param>
public sealed record BlockHeading(string Heading, string Subheading)
{
    public static BlockHeading For(TimelineBlock block) => block.Payload switch
    {
        WindowBlock window => new(window.Application.DisplayName, window.Title),
        ComputerStateBlock state => new(ComputerStateLabel.Of(state.State), Subheading: ""),
        _ => new(block.Label, Subheading: ""),
    };
}
