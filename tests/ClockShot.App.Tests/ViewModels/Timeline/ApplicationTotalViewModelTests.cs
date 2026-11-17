using ClockShot.App.ViewModels.Timeline;
using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.App.Tests.ViewModels.Timeline;

public sealed class ApplicationTotalViewModelTests
{
    private const long EditorId = 1;
    private const long BrowserId = 2;

    private static readonly Dictionary<long, TrackedApplication> Applications = new()
    {
        [EditorId] = new(EditorId, "Editor.exe", @"C:\Editor.exe", "Editor", "#3E8E5E"),
        [BrowserId] = new(BrowserId, "Browser.exe", @"C:\Browser.exe", "Browser", "#8E3E5E"),
    };

    [Fact]
    public void EachRowShowsItsApplicationAndTime()
    {
        // Arrange
        ApplicationTotal[] totals = [new(EditorId, TimeSpan.FromMinutes(95))];

        // Act
        var rows = ApplicationTotalViewModel.From(totals, Applications);

        // Assert
        var expected = new ApplicationTotalViewModel(
            Name: "Editor",
            ColorHex: "#3E8E5E",
            TimeText: "1 h 35 min",
            Fraction: 1);
        Assert.Equal(expected, Assert.Single(rows));
    }

    [Fact]
    public void EachBarIsAShareOfTheLongestTimeWhateverTheOrder()
    {
        // Arrange
        ApplicationTotal[] totals = [new(BrowserId, TimeSpan.FromMinutes(15)), new(EditorId, TimeSpan.FromMinutes(60))];

        // Act
        var rows = ApplicationTotalViewModel.From(totals, Applications);

        // Assert
        Assert.Equal([0.25, 1], rows.Select(row => row.Fraction));
    }

    [Fact]
    public void AnUnknownApplicationIsLeftOutAndDoesNotSetTheLongestTime()
    {
        // Arrange
        const long UnknownId = 99;
        ApplicationTotal[] totals = [new(UnknownId, TimeSpan.FromMinutes(60)), new(EditorId, TimeSpan.FromMinutes(15))];

        // Act
        var rows = ApplicationTotalViewModel.From(totals, Applications);

        // Assert
        Assert.Equal(1, Assert.Single(rows).Fraction);
    }

    [Fact]
    public void NoTotalsGiveNoRows()
    {
        // Act
        var rows = ApplicationTotalViewModel.From([], Applications);

        // Assert
        Assert.Empty(rows);
    }
}
