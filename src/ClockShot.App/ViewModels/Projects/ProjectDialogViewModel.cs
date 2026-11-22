using System.Windows.Input;

using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Data.Projects;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClockShot.App.ViewModels.Projects;

/// <summary>
/// The project form, which the tag editor and the Projects tab use to add or edit a project.
/// It has the project's name, its client and the project it sits inside.
/// </summary>
public sealed partial class ProjectDialogViewModel : DialogViewModel
{
    private readonly ProjectRepository _projects;
    private readonly ProjectCatalog _catalog;
    private readonly Project? _editing;

    private ProjectDialogViewModel(
        ProjectRepository projects,
        ProjectCatalog catalog,
        Project? editing,
        string? name,
        Project? parent,
        Client? client)
        : base(editing is null ? "New project" : "Edit project")
    {
        _projects = projects;
        _catalog = catalog;
        _editing = editing;
        ClientNames = [.. catalog.Clients
            .Where(c => !c.IsArchived)
            .Select(c => c.Name)
            .Order(StringComparer.CurrentCultureIgnoreCase)];
        Name = name?.Trim() ?? "";
        ClientText = (parent is not null ? catalog.ClientOf(parent) : client)?.Name;
        RefreshParentOptions();
        SelectedParent = ParentOptions.FirstOrDefault(o => o.Project?.Id == parent?.Id) ?? ParentOptions[0];
    }

    public bool IsEditing => _editing is not null;

    public override string ConfirmText => IsEditing ? "Save" : "Create project";

    public override ICommand ConfirmCommand => SaveCommand;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; }

    /// <summary>
    /// Existing client names, for the client box's suggestions.
    /// </summary>
    public IReadOnlyList<string> ClientNames { get; }

    /// <summary>
    /// Blank means no client, and a new name creates the client.
    /// A project inside another uses that project's client.
    /// </summary>
    [ObservableProperty]
    public partial string? ClientText { get; set; }

    /// <summary>
    /// "None (top-level project)", then the chosen client's projects.
    /// The project being edited and the projects inside it are left out.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<ParentOptionViewModel> ParentOptions { get; private set; } = [];

    [ObservableProperty]
    public partial ParentOptionViewModel? SelectedParent { get; set; }

    [ObservableProperty]
    public partial string Error { get; private set; } = "";

    /// <summary>
    /// The project once the form has been saved, so the caller can select it.
    /// </summary>
    public Project? Saved { get; private set; }

    /// <summary>
    /// A form for a new project.
    /// </summary>
    public static ProjectDialogViewModel ForNew(
        ProjectRepository projects,
        ProjectCatalog catalog,
        string? name = null,
        Project? parent = null,
        Client? client = null) =>
        new(projects, catalog, editing: null, name, parent, client);

    /// <summary>
    /// A form for changing an existing project.
    /// </summary>
    public static ProjectDialogViewModel ForEdit(
        ProjectRepository projects,
        ProjectCatalog catalog,
        Project project) =>
        new(
            projects,
            catalog,
            project,
            project.Name,
            catalog.ProjectById(project.ParentId),
            catalog.ClientOf(project));

    partial void OnClientTextChanged(string? value) => RefreshParentOptions();

    [RelayCommand(CanExecute = nameof(HasName))]
    private async Task SaveAsync()
    {
        Error = "";
        try
        {
            var parent = SelectedParent?.Project;
            var clientId = parent is null ? (await ClientAsync())?.Id : null;
            if (_editing is { } project)
            {
                Saved = project with { Name = Name, ClientId = clientId, ParentId = parent?.Id };
                await _projects.UpdateProjectAsync(Saved);
            }
            else
            {
                Saved = await _projects.AddProjectAsync(Name, clientId, parent?.Id);
            }

            CloseCommand.Execute(null);
        }
        catch (Exception ex) when (ex is DuplicateNameException or ArgumentException)
        {
            Saved = null;
            Error = ex.Message;
        }
    }

    private bool HasName() => !string.IsNullOrWhiteSpace(Name);

    /// <summary>
    /// The client named in the box, creating it if it's new.
    /// It's null when the box is blank.
    /// </summary>
    private async Task<Client?> ClientAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientText))
        {
            return null;
        }

        return _catalog.ClientNamed(ClientText) ?? await _projects.AddClientAsync(ClientText);
    }

    private void RefreshParentOptions()
    {
        var client = _catalog.ClientNamed(ClientText);
        // A new client has no projects yet.
        var candidates = string.IsNullOrWhiteSpace(ClientText) || client is not null
            ? _catalog.Projects.Where(p =>
                _catalog.ClientOf(p)?.Id == client?.Id && !_catalog.IsArchived(p) && !IsSelfOrInside(p))
            : [];
        var previous = SelectedParent?.Project?.Id;
        ParentOptions = [
            new ParentOptionViewModel(Project: null, "None (top-level project)"),
            .. candidates
                .Select(p => new ParentOptionViewModel(p, _catalog.PathOf(p)))
                .OrderBy(o => o.Label, StringComparer.CurrentCultureIgnoreCase)];
        SelectedParent = ParentOptions.FirstOrDefault(o => o.Project?.Id == previous) ?? ParentOptions[0];
    }

    /// <summary>
    /// A project can't be moved inside itself or a project that's inside it.
    /// </summary>
    private bool IsSelfOrInside(Project candidate) =>
        _editing is { } editing
        && (candidate.Id == editing.Id || _catalog.AncestorsOf(candidate).Any(a => a.Id == editing.Id));
}
