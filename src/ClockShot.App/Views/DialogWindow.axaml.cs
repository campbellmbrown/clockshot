using Avalonia.Controls;

namespace ClockShot.App.Views;

public sealed partial class DialogWindow : Window
{
    /// <summary>
    /// The largest share of the screen's height a dialog may take.
    /// </summary>
    private const double MaxScreenShare = 0.85;

    public DialogWindow() => InitializeComponent();

    /// <summary>
    /// A dialog sizes itself to its content, but it's never taller than most of the screen.
    /// A view with long content scrolls it.
    /// </summary>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (Screens.ScreenFromWindow(this) is { } screen)
        {
            MaxHeight = screen.WorkingArea.Height / screen.Scaling * MaxScreenShare;
        }
    }
}
