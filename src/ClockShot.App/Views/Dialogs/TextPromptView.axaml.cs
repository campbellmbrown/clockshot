using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClockShot.App.Views.Dialogs;

public sealed partial class TextPromptView : UserControl
{
    public TextPromptView() => InitializeComponent();

    /// <summary>
    /// Focuses the box and selects its text.
    /// Typing then replaces the text.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        TextBox.Focus();
        TextBox.SelectAll();
    }
}
