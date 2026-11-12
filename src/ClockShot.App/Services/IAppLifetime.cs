namespace ClockShot.App.Services;

/// <summary>
/// Lets view models end the app without knowing about Avalonia's lifetime classes.
/// </summary>
public interface IAppLifetime
{
    void Shutdown();
}
