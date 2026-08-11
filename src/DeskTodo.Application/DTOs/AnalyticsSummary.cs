using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.DTOs;

/// <summary>
/// Phase 24's Dashboard summary tiles. All rates are 0–100. Weekly/Monthly/Overall
/// completion rates are computed from <see cref="TaskItem.PlanDate"/> (which day a
/// task was *for*), matching how the rest of the app organizes by plan date; the streak is
/// computed from <see cref="TaskItem.CompletedAt"/> instead (which day it was
/// *actually finished*) — see <c>AnalyticsService.ComputeCurrentStreak</c>'s doc comment for
/// why those two are deliberately different dates.
/// </summary>
public sealed record AnalyticsSummary
{
    public required double WeeklyCompletionRate { get; init; }

    public required double MonthlyCompletionRate { get; init; }

    public required double OverallCompletionRate { get; init; }

    /// <summary>Consecutive days (walking back from today) with at least one task completed — see <see cref="Goal.GetCurrentStreak"/> for the identical algorithm this mirrors.</summary>
    public required int CurrentStreakDays { get; init; }

    public required int FocusMinutesThisWeek { get; init; }

    public required int FocusMinutesAllTime { get; init; }
}
