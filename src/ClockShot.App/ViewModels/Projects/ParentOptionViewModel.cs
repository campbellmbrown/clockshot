using ClockShot.Core.Models;

namespace ClockShot.App.ViewModels.Projects;

/// <summary>
/// A choice in the project form's "Inside" box.
/// A null project means a top-level project.
/// </summary>
public sealed record ParentOptionViewModel(Project? Project, string Label);
