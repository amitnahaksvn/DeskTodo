using DeskTodo.Application.DTOs;

namespace DeskTodo.Application.Services;

/// <summary>
/// Phase 24's read-only analytics — every method aggregates over data <see cref="ITaskService"/>/
/// <see cref="IFocusSessionService"/> already persist elsewhere; nothing here writes anything.
/// </summary>
public interface IAnalyticsService
{
    Task<AnalyticsSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>One row per day in [<paramref name="from"/>, <paramref name="to"/>], inclusive — the Dashboard's Heat Map.</summary>
    Task<IReadOnlyList<DailyCompletionCount>> GetHeatMapDataAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>One row per category with at least one task, most active first, plus a "No Category" row if applicable.</summary>
    Task<IReadOnlyList<CategoryAnalytics>> GetCategoryAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>A Markdown-formatted Weekly/Monthly Report for [<paramref name="periodStart"/>, <paramref name="periodEnd"/>], inclusive.</summary>
    Task<string> GenerateReportAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default);
}
