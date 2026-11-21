using Avalonia.Controls;

using ClockShot.Core.Models;
using ClockShot.Core.Projects;

namespace ClockShot.App.ViewModels.Tagging;

/// <summary>
/// A project as the project picker offers it.
/// Its client and the projects it sits inside show in grey below its name.
/// </summary>
public sealed record ProjectOptionViewModel(Project Project, string Name, string Context, string SearchText)
{
    public bool HasContext => Context.Length > 0;

    public static ProjectOptionViewModel From(Project project, ProjectCatalog catalog) =>
        new(project, project.Name, catalog.ContextOf(project), catalog.PathOf(project));

    /// <summary>
    /// The picker matches every typed word anywhere in the project's path.
    /// </summary>
    public static AutoCompleteFilterPredicate<object?> Filter { get; } = (text, item) =>
        item is ProjectOptionViewModel option && SearchWords.Of(text).AreAllIn(option.SearchText);

    /// <summary>
    /// Shown in the project box once the project is chosen.
    /// </summary>
    public override string ToString() => Name;
}
