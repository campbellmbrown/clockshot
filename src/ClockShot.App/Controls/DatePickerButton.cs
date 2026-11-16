using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

using Calendar = Avalonia.Controls.Calendar;

namespace ClockShot.App.Controls;

/// <summary>
/// A button that shows a date next to the calendar icon, and opens a calendar to choose another.
/// It's used instead of <see cref="CalendarDatePicker"/>, whose built-in icon doesn't match the app's icons.
/// </summary>
public sealed class DatePickerButton : Button
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<DatePickerButton, DateTime?>(
            nameof(SelectedDate),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> DisplayDateEndProperty =
        AvaloniaProperty.Register<DatePickerButton, DateTime?>(nameof(DisplayDateEnd));

    /// <summary>
    /// How the date is shown on the button, e.g. "dddd d MMMM yyyy".
    /// </summary>
    public static readonly StyledProperty<string> DateFormatProperty =
        AvaloniaProperty.Register<DatePickerButton, string>(nameof(DateFormat), defaultValue: "d MMM yyyy");

    private readonly IconText _content = new() { Kind = Icons.PickDate };
    private readonly Calendar _calendar = new();
    private readonly Flyout _flyout;

    public DatePickerButton()
    {
        Content = _content;
        _flyout = new Flyout { Content = _calendar, Placement = PlacementMode.BottomEdgeAlignedLeft };
        _flyout.Opening += (_, _) =>
        {
            _calendar.SelectedDate = SelectedDate;
            _calendar.DisplayDateEnd = DisplayDateEnd;
            if (SelectedDate is { } date)
            {
                _calendar.DisplayDate = date;
            }
        };
        _calendar.SelectedDatesChanged += (_, _) =>
        {
            if (_calendar.SelectedDate is not { } chosen || chosen == SelectedDate)
            {
                return;
            }

            SelectedDate = chosen;
            _flyout.Hide();
        };
        Flyout = _flyout;
        UpdateText();
    }

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime? DisplayDateEnd
    {
        get => GetValue(DisplayDateEndProperty);
        set => SetValue(DisplayDateEndProperty, value);
    }

    public string DateFormat
    {
        get => GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(Button);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedDateProperty || change.Property == DateFormatProperty)
        {
            UpdateText();
        }
    }

    private void UpdateText() =>
        _content.Text = SelectedDate is { } date ? date.ToString(DateFormat, CultureInfo.CurrentCulture) : "Choose a day";
}
