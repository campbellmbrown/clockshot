using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.App.ViewModels.Tagging;

/// <summary>
/// Asks what the user was doing while away, with the usual tag editor.
/// Tagging closes it.
/// Skip or closing the window leaves the time untagged.
/// </summary>
public sealed class AwayPromptViewModel : DialogViewModel
{
    public AwayPromptViewModel(TagEditorViewModel editor, TimeRange away, TimeZoneInfo zone)
        : base("Welcome back")
    {
        Editor = editor;
        Message = $"You were away {LocalTime.FormatRange(away, zone)} " +
                  $"({DurationText.Format(away.Duration)}). What were you doing?";
        editor.TagNew(away);
        editor.Applied += () => CloseCommand.Execute(null);
    }

    public TagEditorViewModel Editor { get; }

    public string Message { get; }

    public override string CloseText => "Skip, leave it untagged";

    /// <summary>
    /// Enter belongs to the tag editor.
    /// </summary>
    public override bool ClosesOnEnter => false;
}
