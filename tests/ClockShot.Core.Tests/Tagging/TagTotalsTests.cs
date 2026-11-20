using ClockShot.Core.Models;
using ClockShot.Core.Tagging;

namespace ClockShot.Core.Tests.Tagging;

public sealed class TagTotalsTests
{
    private const long ProjectA = 1;
    private const long ProjectB = 2;
    private const long TaskId = 7;
    private static readonly DateTime Nine = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TotalsSumTimePerProjectAndTaskInsideTheRange()
    {
        // Arrange
        TimeEntry[] entries =
        [
            new(Id: 1, Minutes(0, 30), ProjectA, TaskId: null, Note: null),
            new(Id: 2, Minutes(30, 40), ProjectA, TaskId, Note: null),
            new(Id: 3, Minutes(40, 90), ProjectB, TaskId: null, Note: null),
        ];

        // Act
        var totals = TagTotals.Of(entries, Minutes(0, 60));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(40), totals.ForProjectOnly(ProjectA));
        Assert.Equal(TimeSpan.FromMinutes(10), totals.ForTask(ProjectA, TaskId));
        Assert.Equal(TimeSpan.FromMinutes(20), totals.ForProjectOnly(ProjectB));
    }

    [Fact]
    public void EmptyHasNoTime()
    {
        // Act
        var total = TagTotals.Empty.ForProjectOnly(ProjectA);

        // Assert
        Assert.Equal(TimeSpan.Zero, total);
    }

    private static TimeRange Minutes(int from, int to) => new(Nine.AddMinutes(from), Nine.AddMinutes(to));
}
