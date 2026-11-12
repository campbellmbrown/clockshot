namespace ClockShot.Core.Settings;

public interface ISettingsStore
{
    AppSettings Current { get; }

    void Save(AppSettings settings);

    /// <summary>
    /// Raised after <see cref="Save"/>, so running services can pick up new values.
    /// </summary>
    event Action<AppSettings>? Changed;
}
