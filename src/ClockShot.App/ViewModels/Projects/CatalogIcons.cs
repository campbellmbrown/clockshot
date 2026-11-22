using ClockShot.App.Controls;
using ClockShot.Core.Projects;

namespace ClockShot.App.ViewModels.Projects;

/// <summary>
/// The icon for each kind of row, the same in the Projects tree and the timesheet.
/// </summary>
public static class CatalogIcons
{
    public static string For(CatalogRowKind kind) => kind switch
    {
        CatalogRowKind.Client => Icons.Client,
        CatalogRowKind.NoClient or CatalogRowKind.Project => Icons.Project,
        CatalogRowKind.Task => Icons.Task,
        CatalogRowKind.General => Icons.General,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null),
    };
}
