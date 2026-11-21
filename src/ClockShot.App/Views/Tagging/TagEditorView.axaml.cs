using Avalonia.Controls;

using ClockShot.App.ViewModels.Tagging;

namespace ClockShot.App.Views.Tagging;

public sealed partial class TagEditorView : UserControl
{
    private TagEditorViewModel? _editor;

    public TagEditorView() => InitializeComponent();

    /// <summary>
    /// Moves keyboard focus to the project box when the view model asks.
    /// It asks when Enter is pressed on the timeline or a Tag menu item is chosen.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _editor?.FocusRequested -= FocusProjectBox;
        _editor = DataContext as TagEditorViewModel;
        _editor?.FocusRequested += FocusProjectBox;
    }

    private void FocusProjectBox() => ProjectBox.Focus();
}
