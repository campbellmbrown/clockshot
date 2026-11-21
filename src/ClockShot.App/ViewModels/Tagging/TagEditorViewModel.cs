using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels.Tagging;

/// <summary>
/// Tags the selected time with a project.
/// </summary>
public sealed partial class TagEditorViewModel(Tagger tagger) : ViewModelBase
{
    private TimeZoneInfo _zone = TimeZoneInfo.Utc;

    /// <summary>
    /// Raised after tagging succeeds.
    /// </summary>
    public event Action? Applied;

    /// <summary>
    /// The time being tagged, or null when nothing is selected.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial TimeRange? Target { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<ProjectOptionViewModel> ProjectOptions { get; private set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial ProjectOptionViewModel? SelectedProject { get; set; }

    /// <summary>
    /// Called when the projects change.
    /// </summary>
    public void SetCatalog(ProjectCatalog catalog, TimeZoneInfo zone)
    {
        _zone = zone;
        var selectedId = SelectedProject?.Project.Id;
        ProjectOptions = [.. catalog.UnarchivedProjects().Select(p => new ProjectOptionViewModel(p))];
        SelectedProject = ProjectOptions.FirstOrDefault(o => o.Project.Id == selectedId);
    }

    /// <summary>
    /// Prepares to tag new time.
    /// It keeps the last project chosen, which is usually what's wanted next.
    /// </summary>
    public void TagNew(TimeRange range) => Target = range;

    public void Clear() => Target = null;

    /// <summary>
    /// Tags the target time.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        if (Target is not { } range || SelectedProject is not { } option)
        {
            return;
        }

        await tagger.TagAsync(
            range,
            option.Project.Id,
            taskId: null,
            note: null,
            $"Undo tagging {RangeText(range)}");
        Applied?.Invoke();
    }

    private bool CanApply() => Target is not null && SelectedProject is not null;

    private string RangeText(TimeRange range) => LocalTime.FormatRange(range, _zone);
}
