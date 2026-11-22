using ClockShot.App.ViewModels;

namespace ClockShot.App.Services;

/// <summary>
/// Shows dialogs from view models without them knowing about windows.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows the dialog over the main window, blocking it, and completes when the dialog closes.
    /// </summary>
    Task ShowAsync(DialogViewModel dialog);

    /// <summary>
    /// Shows the dialog on top of other windows without blocking anything.
    /// It suits a prompt that appears while the user is working in another app.
    /// </summary>
    void ShowWithoutBlocking(DialogViewModel dialog);
}
