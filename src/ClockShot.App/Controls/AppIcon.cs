using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform;

namespace ClockShot.App.Controls;

/// <summary>
/// The app's icon for windows and the tray, from the files in Assets/AppIcon.
/// </summary>
public static class AppIcon
{
    private const string Folder = "avares://ClockShot/Assets/AppIcon/";

    /// <summary>
    /// Picks the icon for whether tracking is paused, for window icons bound in XAML.
    /// </summary>
    public static readonly IValueConverter FromIsPaused = new FuncValueConverter<bool, WindowIcon>(For);

    public static WindowIcon Normal { get; } = Load("app.ico");

    public static WindowIcon Paused { get; } = Load("app-paused.ico");

    public static WindowIcon For(bool isPaused) => isPaused ? Paused : Normal;

    private static WindowIcon Load(string fileName)
    {
        using var stream = AssetLoader.Open(new Uri(Folder + fileName));
        return new WindowIcon(stream);
    }
}
