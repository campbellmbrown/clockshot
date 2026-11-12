namespace ClockShot.App.Tests;

internal static class RepositoryFolder
{
    /// <summary>
    /// The folder that holds ClockShot.slnx.
    /// </summary>
    public static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "ClockShot.slnx")))
            {
                return folder.FullName;
            }
        }
        throw new DirectoryNotFoundException("ClockShot.slnx not found above " + AppContext.BaseDirectory);
    }
}
