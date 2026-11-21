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
/// Tags the selected time with a project and a task, or edits or deletes an existing tag.
/// A blank task puts the time in the project's General bucket, and a new task name creates the task.
/// The colour belongs to the project and task pair, so every tag with that pair shares it.
/// </summary>
public sealed partial class TagEditorViewModel(
    ProjectRepository projects,
    ITimeEntryRepository entries,
    Tagger tagger,
    IDialogService dialogs) : ViewModelBase
{
    private const int RecentTagCount = 6;

    private ProjectCatalog _catalog = ProjectCatalog.Empty;
    private TimeZoneInfo _zone = TimeZoneInfo.Utc;

    /// <summary>
    /// Raised when the editor wants keyboard focus in the project box, such as when Enter is pressed on the timeline.
    /// </summary>
    public event Action? FocusRequested;

    /// <summary>
    /// Raised after tagging or saving changes succeeds.
    /// </summary>
    public event Action? Applied;

    /// <summary>
    /// The time being tagged, or null when nothing is selected.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial TimeRange? Target { get; private set; }

    /// <summary>
    /// The existing tag being edited, or null when tagging new time.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(ApplyText), nameof(Title))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial TimeEntry? Editing { get; private set; }

    public bool IsEditing => Editing is not null;

    public string Title => IsEditing ? "Tag" : "Tag this time";

    public string ApplyText => IsEditing ? "Save changes" : "Tag";

    [ObservableProperty]
    public partial IReadOnlyList<ProjectOptionViewModel> ProjectOptions { get; private set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand), nameof(EditProjectCommand))]
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
    /// Whether the tag uses the colour from its name, the way applications do, instead of a chosen one.
    /// </summary>
    public bool UseAutomaticColor
    {
        get => ChosenColorHex is null;

        // Unticking keeps the colour shown, which becomes the chosen one.
        set => ChosenColorHex = value ? null : ColorHex;
    }

    /// <summary>
    /// The colour in the colour picker, which is the chosen colour or else the automatic one.
    /// Picking a different colour chooses it.
    /// </summary>
    public string ColorHex
    {
        get => ChosenColorHex ?? AutomaticColor();
        set
        {
            // The picker also writes back the colour it was shown, which isn't the user choosing it.
            // With no project there's no tag to colour, and the picker writes back grey.
            if (SelectedProject is not null && !string.Equals(value, ColorHex, StringComparison.OrdinalIgnoreCase))
            {
                ChosenColorHex = value;
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecentTags))]
    public partial IReadOnlyList<RecentTagViewModel> RecentTags { get; private set; } = [];

    public bool HasRecentTags => RecentTags.Count > 0;

    /// <summary>
    /// Why the last action didn't work, such as a name that's already taken.
    /// It's empty when the last action worked.
    /// </summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = "";

    /// <summary>
    /// The colour chosen for the project and task pair, or null to use the automatic one.
    /// It's saved when the tag is.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseAutomaticColor), nameof(ColorHex))]
    private partial string? ChosenColorHex { get; set; }

    public void RequestFocus() => FocusRequested?.Invoke();

    /// <summary>
    /// Called when the projects change.
    /// </summary>
    public async Task SetCatalogAsync(ProjectCatalog catalog, TimeZoneInfo zone)
    {
        _catalog = catalog;
        _zone = zone;
        var selectedId = SelectedProject?.Project.Id;
        ProjectOptions = [.. catalog.UnarchivedProjects().Select(p => ProjectOptionViewModel.From(p, catalog))];
        SelectedProject = ProjectOptions.FirstOrDefault(o => o.Project.Id == selectedId);
        await RefreshRecentTagsAsync();
    }

    /// <summary>
    /// Prepares to tag new time.
    /// It keeps the last project and task chosen, which is usually what's wanted next.
    /// </summary>
    public void TagNew(TimeRange range)
    {
        // A note describes one tag, so the tag that was being edited keeps its note to itself.
        if (Editing is not null)
        {
            Note = null;
        }

        Target = range;
        Editing = null;
        Error = "";
    }

    /// <summary>
    /// Prepares to edit an existing tag.
    /// </summary>
    public void Edit(TimeEntry entry)
    {
        Target = entry.Range;
        Editing = entry;
        SelectedProject = ProjectOptions.FirstOrDefault(o => o.Project.Id == entry.ProjectId);
        TaskText = _catalog.TaskById(entry.TaskId)?.Name;
        Note = entry.Note;
        Error = "";
    }

    public void Clear()
    {
        Target = null;
        Editing = null;
    }

    /// <summary>
    /// Reloads the recently used tags, such as after tagging.
    /// </summary>
    public async Task RefreshRecentTagsAsync()
    {
        // Twice as many, so enough are left once the tags of archived projects are dropped.
        var recent = await entries.GetRecentTagsAsync(RecentTagCount * 2);
        RecentTags = [.. recent
            .Where(tag => _catalog.ProjectById(tag.ProjectId) is { } project && !_catalog.IsArchived(project))
            .Take(RecentTagCount)
            .Select(tag => new RecentTagViewModel(
                tag.ProjectId,
                tag.TaskId,
                _catalog.TagLabel(tag.ProjectId, tag.TaskId),
                _catalog.ColorOf(tag.ProjectId, tag.TaskId)))];
    }

    partial void OnTaskTextChanged(string? value) => ShowStoredColor();

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

        ShowStoredColor();
    }

    /// <summary>
    /// Shows the colour already chosen for the project and task in the boxes, or the automatic one.
    /// </summary>
    private void ShowStoredColor()
    {
        ChosenColorHex = SelectedProject is { } option
            ? _catalog.ChosenColorOf(option.Project.Id, _catalog.TaskNamed(option.Project.Id, TaskText)?.Id)
            : null;

        // The automatic colour comes from the names, so it can change even when the chosen one doesn't.
        OnPropertyChanged(nameof(ColorHex));
    }

    /// <summary>
    /// The colour from the tag's name.
    /// It's empty when no project is chosen, which the colour picker shows as grey.
    /// </summary>
    private string AutomaticColor() =>
        SelectedProject is { } option
            ? ProjectCatalog.AutomaticColorOf(ProjectCatalog.LabelOf(option.Project.Name, TaskText))
            : "";

    [RelayCommand]
    private void UseRecentTag(RecentTagViewModel recent)
    {
        SelectedProject = ProjectOptions.FirstOrDefault(o => o.Project.Id == recent.ProjectId);
        TaskText = _catalog.TaskById(recent.TaskId)?.Name;
    }

    /// <summary>
    /// Tags the target time, or saves changes to the tag being edited.
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
            if (ChosenColorHex != _catalog.ChosenColorOf(option.Project.Id, taskId))
            {
                await projects.SetTagColorAsync(option.Project.Id, taskId, ChosenColorHex);
            }

            if (Editing is { } entry)
            {
                await tagger.RetagAsync(
                    entry,
                    option.Project.Id,
                    taskId,
                    Note,
                    $"Undo changing the tag {RangeText(entry.Range)}");
            }
            else
            {
                await tagger.TagAsync(range, option.Project.Id, taskId, Note, $"Undo tagging {RangeText(range)}");

                // The next tag starts without this one's note.
                Note = null;
            }

            Applied?.Invoke();
        }
        catch (DuplicateNameException ex)
        {
            Error = ex.Message;
        }
    }

    private bool CanApply() => Target is not null && SelectedProject is not null;

    [RelayCommand(CanExecute = nameof(IsEditing))]
    private async Task DeleteAsync()
    {
        if (Editing is { } entry)
        {
            await tagger.DeleteAsync(entry, $"Undo deleting the tag {RangeText(entry.Range)}");
        }
    }

    /// <summary>
    /// Opens the project form for a new project, starting with whatever was typed in the project box.
    /// </summary>
    [RelayCommand]
    private Task NewProjectAsync() =>
        ShowProjectFormAsync(
            ProjectDialogViewModel.ForNew(projects, _catalog, SelectedProject is null ? ProjectText : null));

    /// <summary>
    /// Opens the project form for the chosen project.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasProject))]
    private Task EditProjectAsync() =>
        ShowProjectFormAsync(ProjectDialogViewModel.ForEdit(projects, _catalog, SelectedProject!.Project));

    private bool HasProject() => SelectedProject is not null;

    private async Task ShowProjectFormAsync(ProjectDialogViewModel form)
    {
        await dialogs.ShowAsync(form);
        if (form.Saved is not { } saved)
        {
            return;
        }

        await SetCatalogAsync(await projects.LoadCatalogAsync(), _zone);
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
