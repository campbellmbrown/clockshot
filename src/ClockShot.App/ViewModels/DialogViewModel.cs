using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels;

/// <summary>
/// A dialog, shown with <see cref="Services.IDialogService"/>.
/// DialogWindow draws its title and buttons. The view for its body is mapped in App.axaml.
/// </summary>
public abstract partial class DialogViewModel(string title, bool canResize = false) : ViewModelBase
{
    /// <summary>
    /// Raised when the dialog wants to close. The dialog service closes the window.
    /// </summary>
    public event Action? CloseRequested;

    /// <summary>
    /// The window title, also shown as the heading.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// True for dialogs with a lot to show, so the window can be made bigger.
    /// Their views scroll their own content.
    /// </summary>
    public bool CanResize { get; } = canResize;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
