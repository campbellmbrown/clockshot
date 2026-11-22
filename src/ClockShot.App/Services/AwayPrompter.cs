using Avalonia.Threading;

using ClockShot.App.ViewModels.Tagging;
using ClockShot.Core.Models;
using ClockShot.Core.Settings;
using ClockShot.Core.Tracking;
using ClockShot.Data.Projects;

namespace ClockShot.App.Services;

/// <summary>
/// Asks what the user was doing when they come back after being away long enough.
/// The Tracking settings say how long that is.
/// The prompt doesn't block the rest of the app.
/// </summary>
public sealed class AwayPrompter(
    TrackingLoop tracking,
    ISettingsStore settings,
    ProjectRepository projects,
    IDialogService dialogs,
    Func<TagEditorViewModel> createEditor,
    TimeProvider time,
    BackgroundWork background)
{
    private bool _isPromptOpen;

    /// <summary>
    /// Starts listening.
    /// The desktop app calls this at startup.
    /// </summary>
    public void Start() =>
        tracking.ReturnedFromAway += away => Dispatcher.UIThread.Post(() => background.Run(() => PromptAsync(away)));

    private async Task PromptAsync(TimeRange away)
    {
        if (!AwayPromptPolicy.ShouldAsk(settings.Current.Tracking, away, _isPromptOpen))
        {
            return;
        }

        // The prompt opens before anything is awaited, so two quick returns can't open two prompts.
        _isPromptOpen = true;
        var editor = createEditor();
        var prompt = new AwayPromptViewModel(editor, away, time.LocalTimeZone);
        prompt.Closed += () => _isPromptOpen = false;
        dialogs.ShowWithoutBlocking(prompt);

        await editor.SetCatalogAsync(await projects.LoadCatalogAsync(), time.LocalTimeZone);
    }
}
