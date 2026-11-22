using System.IO.Abstractions;

using ClockShot.App.Logging;
using ClockShot.App.Services;
using ClockShot.App.ViewModels;
using ClockShot.App.ViewModels.Projects;
using ClockShot.App.ViewModels.Tagging;
using ClockShot.App.ViewModels.Timeline;
using ClockShot.App.ViewModels.Timesheets;
using ClockShot.Core.Screenshots;
using ClockShot.Core.Settings;
using ClockShot.Core.Tagging;
using ClockShot.Core.Timeline;
using ClockShot.Core.Tracking;
using ClockShot.Data;
using ClockShot.Data.Projects;
using ClockShot.Data.Screenshots;
using ClockShot.Data.Settings;
using ClockShot.Data.Sqlite;
using ClockShot.Data.Tagging;
using ClockShot.Data.Tracking;
using ClockShot.Platform.Windows;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace ClockShot.App;

/// <summary>
/// The one place where concrete types are chosen and wired together.
/// </summary>
internal static class CompositionRoot
{
    /// <param name="paths">Where the data and logs live.</param>
    public static ServiceProvider Build(AppPaths paths)
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton(paths);
        services.AddSingleton(paths.Data);
        services.AddSingleton(TimeProvider.System);
        services.AddLogging(logging => logging.AddSerilog(LogFiles.Create(paths.Logs), dispose: true));
        services.AddSingleton(sp =>
        {
            var database = new SqliteDatabase(sp.GetRequiredService<DataPaths>().Database);
            database.Migrate();
            return database;
        });
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();

        // Tracking
        services.AddSingleton<ISystemSampler, WindowsSystemSampler>();
        services.AddSingleton<IWindowCapturer, WindowsWindowCapturer>();
        services.AddSingleton<ITrackingSink, TrackingWriter>();
        services.AddSingleton<IScreenshotStore, ScreenshotStore>();
        services.AddSingleton<TrackingLoop>();
        services.AddSingleton<ScreenshotCleanup>();
        services.AddSingleton<IActivityHistory, ActivityHistory>();

        // Projects and tagging
        services.AddSingleton<ProjectRepository>();
        services.AddSingleton<ITimeEntryRepository, TimeEntryRepository>();
        services.AddSingleton<Tagger>();

        // App services
        services.AddSingleton<IAppLifetime, DesktopAppLifetime>();
        services.AddSingleton<IShell, SystemShell>();
        services.AddSingleton<ScreenshotImageLoader>();
        services.AddSingleton<MainWindowHost>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<BackgroundWork>();
        services.AddSingleton<AwayPrompter>();
        services.AddSingleton<DayClock>();

        // View models
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TrayIconViewModel>();
        services.AddSingleton<TrackingStatusViewModel>();
        services.AddSingleton<TimelineViewModel>();
        services.AddTransient<ScreenshotPreviewViewModel>();
        services.AddTransient<SelectionDetailsViewModel>();
        services.AddTransient<TagEditorViewModel>();
        services.AddSingleton<Func<TagEditorViewModel>>(sp => sp.GetRequiredService<TagEditorViewModel>);
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<TimesheetViewModel>();
        services.AddSingleton<ProjectsViewModel>();
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
