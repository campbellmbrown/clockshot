using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels.Dialogs;

/// <summary>
/// Asks for one line of text, e.g. a new name.
/// <see cref="Result"/> is null if cancelled.
/// </summary>
public sealed partial class TextPromptViewModel(string title, string label, string initialText, string confirmText) : DialogViewModel(title)
{
    public string Label { get; } = label;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    public partial string Text { get; set; } = initialText;

    /// <summary>
    /// The text entered, without spaces at either end.
    /// It's null when the dialog was cancelled.
    /// </summary>
    public string? Result { get; private set; }

    public override string ConfirmText { get; } = confirmText;

    public override ICommand ConfirmCommand => AcceptCommand;

    [RelayCommand(CanExecute = nameof(HasText))]
    private void Accept()
    {
        Result = Text.Trim();
        CloseCommand.Execute(null);
    }

    private bool HasText() => !string.IsNullOrWhiteSpace(Text);
}
