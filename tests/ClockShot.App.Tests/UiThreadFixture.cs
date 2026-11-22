using Avalonia.Threading;

[assembly: AssemblyFixture(typeof(ClockShot.App.Tests.UiThreadFixture))]

namespace ClockShot.App.Tests;

/// <summary>
/// Creates Avalonia's UI thread dispatcher once, before any test runs.
/// Avalonia creates it on first use, and a test that posts to it while another test is creating it can fail.
/// </summary>
public sealed class UiThreadFixture
{
    public UiThreadFixture() => _ = Dispatcher.UIThread;
}
