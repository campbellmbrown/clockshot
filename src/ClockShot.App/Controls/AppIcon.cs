using Avalonia.Controls;
using Avalonia.Platform;

namespace ClockShot.App.Controls;

/// <summary>
/// The app's icon for windows, from the files in Assets/AppIcon.
/// </summary>
public static class AppIcon
{
    private const string Folder = "avares://ClockShot/Assets/AppIcon/";

    public static WindowIcon Normal { get; } = Load("app.ico");

    private static WindowIcon Load(string fileName)
    {
        using var stream = AssetLoader.Open(new Uri(Folder + fileName));
        return new WindowIcon(stream);
    }
}
