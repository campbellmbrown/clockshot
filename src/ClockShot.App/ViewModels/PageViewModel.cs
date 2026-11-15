namespace ClockShot.App.ViewModels;

/// <summary>
/// A top-level tab in the main window.
/// </summary>
/// <param name="title">The text on the tab.</param>
/// <param name="iconKind">The name from <see cref="Controls.Icons"/> of the icon shown next to the title.</param>
public abstract class PageViewModel(string title, string iconKind) : ViewModelBase
{
    public string Title { get; } = title;

    public string IconKind { get; } = iconKind;

    /// <summary>
    /// Called each time the tab is shown.
    /// A page can override it to refresh what changed while it was hidden.
    /// </summary>
    public virtual void OnShown()
    {
        // Most pages keep themselves up to date.
    }
}
