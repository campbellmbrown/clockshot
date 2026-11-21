using ClockShot.Core.Models;

namespace ClockShot.App.ViewModels.Tagging;

/// <summary>
/// A project as the project picker offers it.
/// </summary>
public sealed record ProjectOptionViewModel(Project Project)
{
    /// <summary>
    /// Shown in the project box once the project is chosen.
    /// </summary>
    public override string ToString() => Project.Name;
}
