namespace ClockShot.App.Controls;

/// <summary>
/// The name of every icon the app shows, by what it means.
/// Each value is a file name in Assets/Icons.
/// Use these instead of strings, so a misspelt name doesn't compile.
/// Several meanings can share a file.
/// </summary>
public static class Icons
{
    // ---- App ----
    public const string App = "clock";
    public const string Exit = "door-open";
    public const string About = "info";
    public const string KeyboardShortcuts = "keyboard";
    public const string OpenFolder = "folder-open";

    // ---- Pages ----
    public const string Timeline = "chart-gantt";
    public const string Timesheet = "sheet";
    public const string Projects = "folder-tree";
    public const string Settings = "settings";

    // ---- Tracking ----
    public const string PauseTracking = "circle-pause";
    public const string ResumeTracking = "circle-play";

    // ---- Common actions ----
    public const string Add = "plus";
    public const string Remove = "x";
    public const string Edit = "pencil";
    public const string Delete = "trash";
    public const string Undo = "undo-2";
    public const string Search = "search";
    public const string Archive = "archive";

    // ---- Days and zoom ----
    public const string PreviousDay = "chevron-left";
    public const string NextDay = "chevron-right";
    public const string Today = "calendar-check";
    public const string PickDate = "calendar";
    public const string ZoomIn = "zoom-in";
    public const string ZoomOut = "zoom-out";
    public const string ZoomToSelection = "zoom-in";
    public const string FitWorkingDay = "fold-horizontal";
    public const string ShowWholeDay = "maximize-2";
    public const string ClearSelection = "x";

    // ---- Screenshot preview ----
    public const string ScreenshotPreview = "image";
    public const string SwitchPreview = "chevron-right";
    public const string PausePreview = "lock";
    public const string ResumePreview = "lock-open";
    public const string PreviewBesidePointer = "picture-in-picture-2";
    public const string PreviewBelowTimeline = "panel-bottom";
    public const string PreviewInWindow = "app-window";

    // ---- Clients, projects and tags ----
    public const string Client = "building-complex";
    public const string Project = "folder";
    public const string Task = "list-todo";
    public const string General = "tag";
    public const string NewClient = "building-complex";
    public const string NewProject = "folder-plus";
    public const string NewTask = "list-plus";
    public const string Tag = "tag";
    public const string RecentTags = "rotate-ccw-clock";

    // ---- Timesheet ----
    public const string RowExpanded = "chevron-down";
    public const string RowCollapsed = "chevron-right";
}
