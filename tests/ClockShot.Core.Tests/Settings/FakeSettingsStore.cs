using ClockShot.Core.Settings;

namespace ClockShot.Core.Tests.Settings;

/// <summary>
/// Keeps the settings in memory.
/// </summary>
internal sealed class FakeSettingsStore : ISettingsStore
{
    public event Action<AppSettings>? Changed;

    public AppSettings Current { get; private set; } = new();

    public void Save(AppSettings settings)
    {
        Current = settings;
        Changed?.Invoke(settings);
    }
}
