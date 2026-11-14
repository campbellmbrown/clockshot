using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread.
    /// Events from tracking and other background work can arrive on other threads.
    /// Bound properties must only change on the UI thread.
    /// </summary>
    protected static void OnUiThread(Action action) => Dispatcher.UIThread.Post(action);
}
