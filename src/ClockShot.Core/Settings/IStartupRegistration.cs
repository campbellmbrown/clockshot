namespace ClockShot.Core.Settings;

/// <summary>
/// Whether the app starts when the user signs in.
/// </summary>
public interface IStartupRegistration
{
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
