using System.Windows.Input;

using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Data.Projects;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels.Projects;

/// <summary>
/// The project form, which the tag editor uses to add a project.
/// It asks only for the project's name.
/// </summary>
public sealed partial class ProjectDialogViewModel : DialogViewModel
{
    private readonly ProjectRepository _projects;

    private ProjectDialogViewModel(ProjectRepository projects, string? name)
        : base("New project")
    {
        _projects = projects;
        Name = name?.Trim() ?? "";
    }

    public override string ConfirmText => "Create project";

    public override ICommand ConfirmCommand => SaveCommand;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Error { get; private set; } = "";

    /// <summary>
    /// The project once the form has been saved, so the caller can select it.
    /// </summary>
    public Project? Saved { get; private set; }

    /// <summary>
    /// A form for a new project.
    /// </summary>
    public static ProjectDialogViewModel ForNew(ProjectRepository projects, string? name = null) => new(projects, name);

    [RelayCommand(CanExecute = nameof(HasName))]
    private async Task SaveAsync()
    {
        Error = "";
        try
        {
            Saved = await _projects.AddProjectAsync(Name, clientId: null, parentId: null);

            CloseCommand.Execute(null);
        }
        catch (DuplicateNameException ex)
        {
            Saved = null;
            Error = ex.Message;
        }
    }

    private bool HasName() => !string.IsNullOrWhiteSpace(Name);
}
