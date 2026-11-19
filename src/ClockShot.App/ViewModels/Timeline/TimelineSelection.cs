using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// What's selected on the timeline, which is one block or a stretch of time, and the drag that changes it.
/// It keeps the details panel showing the selection.
/// </summary>
public sealed partial class TimelineSelection(SelectionDetailsViewModel details) : ViewModelBase
{
    private SelectionDrag? _drag;

    /// <summary>
    /// Raised when the selection or the snap preview changes, so the timeline redraws.
    /// </summary>
    public event Action? Changed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial TimeRange? Range { get; private set; }

    public bool HasSelection => Range is not null;

    /// <summary>
    /// The lane of the selected block, which is drawn outlined.
    /// It's null when the selection is a stretch of time dragged across lanes.
    /// </summary>
    [ObservableProperty]
    public partial LaneKind? Lane { get; private set; }

    /// <summary>
    /// The block edge a drag starting here would snap to, drawn as a faint line before pressing.
    /// It's null when a drag wouldn't snap.
    /// </summary>
    [ObservableProperty]
    public partial DateTime? SnapPreviewUtc { get; private set; }

    public bool IsDragging => _drag is not null;

    /// <summary>
    /// A block is selected when its lane and start match, so the block being recorded stays selected as it grows.
    /// </summary>
    public bool IsSelected(TimelineLane lane, TimelineBlock block) =>
        lane.Kind == Lane && block.Range.Start == Range?.Start;

    public void SelectBlock(LaneKind lane, TimelineBlock block)
    {
        Lane = lane;
        Range = block.Range;
        details.ShowBlock(block);
    }

    /// <summary>
    /// Selects a stretch of time that isn't exactly one block.
    /// </summary>
    public void SelectTime(TimeRange range)
    {
        Lane = null;
        Range = range;
        details.ShowTime(range);
    }

    public void Clear()
    {
        Lane = null;
        Range = null;
        details.Clear();
    }

    /// <summary>
    /// Shows the selection again after the lanes reload.
    /// A block on the same lane that starts at the same time is the same block, even if it has grown.
    /// Otherwise the same stretch of time stays selected.
    /// </summary>
    public void Refresh(IReadOnlyList<TimelineLane> lanes)
    {
        // The drag decides the selection until it ends.
        if (Range is not { } range || IsDragging)
        {
            return;
        }

        if (Lane is { } lane && BlockStartingAt(lane) is { } sameBlock)
        {
            SelectBlock(lane, sameBlock);
        }
        else
        {
            SelectTime(range);
        }

        TimelineBlock? BlockStartingAt(LaneKind kind) =>
            lanes.FirstOrDefault(l => l.Kind == kind)?.Blocks.FirstOrDefault(b => b.Range.Start == range.Start);
    }

    public void ShowSnapPreview(DateTime? edge) => SnapPreviewUtc = edge;

    /// <summary>
    /// Starts a new selection from the anchor, after the pointer moved far enough while pressed.
    /// </summary>
    public void StartDrag(DateTime anchor) => _drag = new SelectionDrag(anchor);

    /// <summary>
    /// Moves the dragged end to the pointer's time, while the anchor stays put.
    /// </summary>
    public void DragTo(DateTime pointerTime)
    {
        if (_drag?.RangeTo(pointerTime) is not { } range)
        {
            return;
        }

        SelectTime(range);
    }

    public void EndDrag() => _drag = null;

    partial void OnRangeChanged(TimeRange? value) => Changed?.Invoke();

    partial void OnLaneChanged(LaneKind? value) => Changed?.Invoke();

    partial void OnSnapPreviewUtcChanged(DateTime? value) => Changed?.Invoke();
}
