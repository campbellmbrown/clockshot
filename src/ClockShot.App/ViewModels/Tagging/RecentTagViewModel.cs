namespace ClockShot.App.ViewModels.Tagging;

/// <summary>
/// A recently used tag, offered as a button that fills in its project and task.
/// </summary>
public sealed record RecentTagViewModel(long ProjectId, long? TaskId, string Label, string ColorHex);
