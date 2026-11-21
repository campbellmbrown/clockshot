using ClockShot.App.ViewModels.Tagging;
using ClockShot.Core.Models;
using ClockShot.Core.Projects;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// The details panel, which shows what the selected block or time range is, its tag and the applications used in it.
/// </summary>
public sealed partial class SelectionDetailsViewModel(TagEditorViewModel tagEditor) : ViewModelBase
{
    private ActivityRecords _records = ActivityRecords.Empty;
    private ProjectCatalog _catalog = ProjectCatalog.Empty;
    private TimeZoneInfo _zone = TimeZoneInfo.Utc;

    /// <summary>
    /// Tags the selection, or edits the selected tag.
    /// </summary>
    public TagEditorViewModel TagEditor { get; } = tagEditor;

    [ObservableProperty]
    public partial bool HasSelection { get; private set; }

    /// <summary>
    /// What was selected, which is a tag, an application, a computer state or "Selected time".
    /// </summary>
    [ObservableProperty]
    public partial string Heading { get; private set; } = "";

    /// <summary>
    /// More detail, such as the window title, or empty when there's nothing to add.
    /// </summary>
    [ObservableProperty]
    public partial string Subheading { get; private set; } = "";

    /// <summary>
    /// The local start and end of the selection.
    /// </summary>
    [ObservableProperty]
    public partial string TimeRangeText { get; private set; } = "";

    [ObservableProperty]
    public partial string TimeText { get; private set; } = "";

    [ObservableProperty]
    public partial IReadOnlyList<ApplicationTotalViewModel> Applications { get; private set; } = [];

    /// <summary>
    /// Sets the loaded day's records, projects and time zone, which every selection on that day uses.
    /// </summary>
    public void SetDay(ActivityRecords records, ProjectCatalog catalog, TimeZoneInfo zone)
    {
        _records = records;
        _catalog = catalog;
        _zone = zone;
    }

    public void ShowBlock(TimelineBlock block) => Show(block.Range, BlockHeading.For(block, _catalog));

    /// <summary>
    /// Shows a stretch of time that isn't exactly one block.
    /// </summary>
    public void ShowTime(TimeRange range) => Show(range, new BlockHeading(Heading: "Selected time", Subheading: ""));

    public void Clear() => HasSelection = false;

    private void Show(TimeRange range, BlockHeading heading)
    {
        HasSelection = true;
        Heading = heading.Heading;
        Subheading = heading.Subheading;
        TimeRangeText = LocalTime.FormatRange(range, _zone);
        TimeText = DurationText.Format(range.Duration);

        var totals = ActivitySummary.ByApplication(_records.Activities, range);
        Applications = ApplicationTotalViewModel.From(totals, _records.Applications);
    }
}
