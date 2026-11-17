using ClockShot.Core.Models;
using ClockShot.Core.Timeline;

namespace ClockShot.App.ViewModels.Timeline;

/// <summary>
/// One row of an "applications used" list.
/// It has a name, a colour, a time, and a bar whose length is relative to the longest row.
/// </summary>
public sealed record ApplicationTotalViewModel(string Name, string ColorHex, string TimeText, double Fraction)
{
    public static IReadOnlyList<ApplicationTotalViewModel> From(
        IReadOnlyList<ApplicationTotal> totals,
        IReadOnlyDictionary<long, TrackedApplication> applications)
    {
        // Every recorded application is loaded with the day, so a missing one means the data is inconsistent.
        // Its time is left out rather than failing the whole list.
        var knownTotals = totals.Where(total => applications.ContainsKey(total.ApplicationId)).ToList();
        if (knownTotals.Count == 0)
        {
            return [];
        }

        var longestSeconds = knownTotals.Max(total => total.Duration.TotalSeconds);
        return [.. knownTotals
            .Select(total =>
            {
                var application = applications[total.ApplicationId];
                return new ApplicationTotalViewModel(
                    application.DisplayName,
                    application.ColorHex,
                    DurationText.Format(total.Duration),
                    total.Duration.TotalSeconds / longestSeconds);
            })];
    }
}
