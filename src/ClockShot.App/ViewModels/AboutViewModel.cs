using System.Runtime.InteropServices;

using ClockShot.Core;

namespace ClockShot.App.ViewModels;

public sealed class AboutViewModel() : DialogViewModel($"About {AppInfo.Name}")
{
    public string Description { get; } = "A local, private time tracker.";

    public string Version { get; } = typeof(AboutViewModel).Assembly.GetName().Version?.ToString(3) ?? "unknown";

    public string Runtime { get; } = RuntimeInformation.FrameworkDescription;

    public string AvaloniaVersion { get; } = typeof(Avalonia.Application).Assembly.GetName().Version?.ToString(3) ?? "unknown";

    public IReadOnlyList<string> Credits { get; } =
    [
        "Avalonia UI (MIT)",
        "CommunityToolkit.Mvvm (MIT)",
        "Inter font (OFL-1.1)",
        "Lucide icons (ISC)",
        "Microsoft.Extensions (MIT)",
        "Serilog (Apache-2.0)",
    ];
}
