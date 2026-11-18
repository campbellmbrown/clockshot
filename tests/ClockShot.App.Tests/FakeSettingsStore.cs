using ClockShot.Core.Settings;

namespace ClockShot.App.Tests;

/// <summary>
/// Keeps the settings in memory.
/// </summary>
public sealed class FakeSettingsStore : ISettingsStore
{
    public event Action<AppSettings>? Changed;

    public AppSettings Current { get; private set; } = new();

    public void Save(AppSettings settings)
    {
        Current = settings;
        Changed?.Invoke(settings);
    }
}
