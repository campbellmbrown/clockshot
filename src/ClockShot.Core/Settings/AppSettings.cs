namespace ClockShot.Core.Settings;

/// <summary>
/// All user settings.
/// They can't be changed in place.
/// Change them with <c>with</c> expressions and save them through <see cref="ISettingsStore"/>.
/// Each number's unit is part of its name, so the saved JSON explains itself.
/// </summary>
public sealed record AppSettings
{
    public GeneralSettings General { get; init; } = new();

    /// <summary>
    /// A copy with missing parts set to defaults.
    /// A hand-edited or damaged settings file then can't crash the app.
    /// </summary>
    public AppSettings Normalized()
    {
        var general = General ?? new();
        return this with
        {
            General = general,
        };
    }
}

public sealed record GeneralSettings
{
    /// <summary>
    /// Closing the main window hides it to the tray instead of exiting, so tracking continues.
    /// </summary>
    public bool CloseToTray { get; init; } = true;
}
