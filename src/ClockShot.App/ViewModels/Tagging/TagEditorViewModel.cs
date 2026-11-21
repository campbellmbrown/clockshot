using ClockShot.App.Services;
using ClockShot.App.ViewModels.Projects;
using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Data.Projects;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels.Tagging;

/// <summary>
/// Tags the selected time with a project and a task.
/// A blank task puts the time in the project's General bucket, and a new task name creates the task.
/// </summary>
public sealed partial class TagEditorViewModel(
    ProjectRepository projects,
    Tagger tagger,
    IDialogService dialogs) : ViewModelBase
{
    private ProjectCatalog _catalog = ProjectCatalog.Empty;
    private TimeZoneInfo _zone = TimeZoneInfo.Utc;

    /// <summary>
    /// Raised when the editor wants keyboard focus in the project box, such as when Enter is pressed on the timeline.
    /// </summary>
    public event Action? FocusRequested;

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
    /// What's typed in the project box.
    /// </summary>
    [ObservableProperty]
    public partial string? ProjectText { get; set; }

    /// <summary>
    /// The selected project's tasks, for the task box's suggestions.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> TaskOptions { get; private set; } = [];

    /// <summary>
    /// Blank means the project's General bucket.
    /// </summary>
    [ObservableProperty]
    public partial string? TaskText { get; set; }

    [ObservableProperty]
    public partial string? Note { get; set; }

    /// <summary>
    /// Why the last action didn't work, such as a name that's already taken.
    /// It's empty when the last action worked.
    /// </summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = "";

    public void RequestFocus() => FocusRequested?.Invoke();

    /// <summary>
    /// Called when the projects change.
    /// </summary>
    public void SetCatalog(ProjectCatalog catalog, TimeZoneInfo zone)
    {
        _catalog = catalog;
        _zone = zone;
        var selectedId = SelectedProject?.Project.Id;
        ProjectOptions = [.. catalog.UnarchivedProjects().Select(p => ProjectOptionViewModel.From(p, catalog))];
        SelectedProject = ProjectOptions.FirstOrDefault(o => o.Project.Id == selectedId);
    }

    /// <summary>
    /// Prepares to tag new time.
    /// It keeps the last project and task chosen, which is usually what's wanted next.
    /// </summary>
    public void TagNew(TimeRange range)
    {
        Target = range;
        Error = "";
    }

    public void Clear() => Target = null;

    partial void OnSelectedProjectChanged(ProjectOptionViewModel? value)
    {
        TaskOptions = value is null ? [] : [.. _catalog.TasksOf(value.Project.Id).Select(t => t.Name)];

        // A typed task carries over only when the new project has an unarchived task with that name.
        if (value is not null
            && !string.IsNullOrWhiteSpace(TaskText)
            && _catalog.TaskNamed(value.Project.Id, TaskText) is not { IsArchived: false })
        {
            TaskText = null;
        }
    }

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

        Error = "";
        try
        {
            var taskId = await TaskIdAsync(option.Project.Id);
            await tagger.TagAsync(range, option.Project.Id, taskId, Note, $"Undo tagging {RangeText(range)}");

            // The next tag starts without this one's note.
            Note = null;
            Applied?.Invoke();
        }
        catch (DuplicateNameException ex)
        {
            Error = ex.Message;
        }
    }

    private bool CanApply() => Target is not null && SelectedProject is not null;

    /// <summary>
    /// Opens the project form for a new project, starting with whatever was typed in the project box.
    /// </summary>
    [RelayCommand]
    private Task NewProjectAsync() =>
        ShowProjectFormAsync(ProjectDialogViewModel.ForNew(projects, SelectedProject is null ? ProjectText : null));

    private async Task ShowProjectFormAsync(ProjectDialogViewModel form)
    {
        await dialogs.ShowAsync(form);
        if (form.Saved is not { } saved)
        {
            return;
        }

        SetCatalog(await projects.LoadCatalogAsync(), _zone);
        SelectedProject = ProjectOptions.FirstOrDefault(o => o.Project.Id == saved.Id);
    }

    /// <summary>
    /// The chosen task's id, creating the task if its name is new.
    /// It's null, meaning General, when the task box is blank.
    /// </summary>
    private async Task<long?> TaskIdAsync(long projectId)
    {
        if (string.IsNullOrWhiteSpace(TaskText))
        {
            return null;
        }

        var task = _catalog.TaskNamed(projectId, TaskText) ?? await projects.AddTaskAsync(projectId, TaskText);
        return task.Id;
    }

    private string RangeText(TimeRange range) => LocalTime.FormatRange(range, _zone);
}
