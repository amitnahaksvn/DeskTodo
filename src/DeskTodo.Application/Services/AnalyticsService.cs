using System.Text;
using DeskTodo.Application.DTOs;
using DeskTodo.Domain.Entities;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IAnalyticsService"/>
public sealed class AnalyticsService(ITaskService taskService, IFocusSessionService focusSessionService, TimeProvider timeProvider) : IAnalyticsService
{
    public async Task<AnalyticsSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = Today();
        var tasks = await taskService.GetAllTasksAsync(cancellationToken);
        var activeTasks = tasks.Where(t => !t.IsArchived).ToList();
        var sessions = await focusSessionService.GetAllSessionsAsync(cancellationToken);

        var weekStart = StartOfWeek(today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return new AnalyticsSummary
        {
            WeeklyCompletionRate = CompletionRate(activeTasks, t => t.PlanDate >= weekStart && t.PlanDate < weekStart.AddDays(7)),
            MonthlyCompletionRate = CompletionRate(activeTasks, t => t.PlanDate >= monthStart && t.PlanDate <= monthEnd),
            OverallCompletionRate = CompletionRate(activeTasks, _ => true),
            CurrentStreakDays = ComputeCurrentStreak(activeTasks, today),
            FocusMinutesThisWeek = sessions.Where(s => LocalDate(s.StartedAt) >= weekStart && LocalDate(s.StartedAt) < weekStart.AddDays(7)).Sum(s => s.DurationMinutes),
            FocusMinutesAllTime = sessions.Sum(s => s.DurationMinutes),
        };
    }

    public async Task<IReadOnlyList<DailyCompletionCount>> GetHeatMapDataAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var tasks = await taskService.GetAllTasksAsync(cancellationToken);
        var activeTasks = tasks.Where(t => !t.IsArchived).ToList();

        var completedByDay = activeTasks
            .Where(t => t.IsCompleted && t.CompletedAt is not null)
            .GroupBy(t => LocalDate(t.CompletedAt!.Value))
            .ToDictionary(g => g.Key, g => g.Count());
        var plannedByDay = activeTasks.GroupBy(t => t.PlanDate).ToDictionary(g => g.Key, g => g.Count());

        var days = new List<DailyCompletionCount>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            days.Add(new DailyCompletionCount(
                date,
                completedByDay.GetValueOrDefault(date),
                plannedByDay.GetValueOrDefault(date)));
        }

        return days;
    }

    public async Task<IReadOnlyList<CategoryAnalytics>> GetCategoryAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await taskService.GetAllTasksAsync(cancellationToken);
        var activeTasks = tasks.Where(t => !t.IsArchived).ToList();
        var sessions = await focusSessionService.GetAllSessionsAsync(cancellationToken);

        var focusMinutesByTaskId = sessions
            .Where(s => s.TaskId is not null)
            .GroupBy(s => s.TaskId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));

        return activeTasks
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryAnalytics(
                g.Key,
                g.Key is null ? "No Category" : g.First().Category?.Name ?? "No Category",
                g.Key is null ? "#94A3B8" : g.First().Category?.ColorHex ?? "#94A3B8",
                g.Count(),
                g.Count(t => t.IsCompleted),
                g.Sum(t => focusMinutesByTaskId.GetValueOrDefault(t.Id))))
            .OrderByDescending(c => c.TotalCount)
            .ToList();
    }

    public async Task<string> GenerateReportAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default)
    {
        var tasks = await taskService.GetAllTasksAsync(cancellationToken);
        var periodTasks = tasks
            .Where(t => !t.IsArchived && t.PlanDate >= periodStart && t.PlanDate <= periodEnd)
            .OrderBy(t => t.PlanDate)
            .ThenBy(t => t.DayOrder)
            .ToList();
        var sessions = await focusSessionService.GetAllSessionsAsync(cancellationToken);
        var periodFocusMinutes = sessions
            .Where(s => LocalDate(s.StartedAt) >= periodStart && LocalDate(s.StartedAt) <= periodEnd)
            .Sum(s => s.DurationMinutes);

        var completed = periodTasks.Where(t => t.IsCompleted).ToList();
        var open = periodTasks.Where(t => !t.IsCompleted).ToList();
        var rate = periodTasks.Count == 0 ? 0 : completed.Count * 100.0 / periodTasks.Count;

        var report = new StringBuilder();
        report.AppendLine($"# DeskTodo Report — {periodStart:MMM d, yyyy} to {periodEnd:MMM d, yyyy}");
        report.AppendLine();
        report.AppendLine("## Summary");
        report.AppendLine($"- Tasks completed: {completed.Count} / {periodTasks.Count} ({rate:F0}%)");
        report.AppendLine($"- Focus time logged: {periodFocusMinutes} minutes");
        report.AppendLine();
        report.AppendLine("## Completed");
        if (completed.Count == 0)
        {
            report.AppendLine("_None._");
        }
        else
        {
            foreach (var task in completed)
            {
                report.AppendLine($"- [x] {task.Title} ({task.PlanDate:MMM d})");
            }
        }

        report.AppendLine();
        report.AppendLine("## Still Open");
        if (open.Count == 0)
        {
            report.AppendLine("_None._");
        }
        else
        {
            foreach (var task in open)
            {
                report.AppendLine($"- [ ] {task.Title} ({task.PlanDate:MMM d})");
            }
        }

        return report.ToString();
    }

    private static double CompletionRate(IReadOnlyList<TaskItem> tasks, Func<TaskItem, bool> predicate)
    {
        var scoped = tasks.Where(predicate).ToList();
        return scoped.Count == 0 ? 0 : scoped.Count(t => t.IsCompleted) * 100.0 / scoped.Count;
    }

    /// <summary>
    /// Consecutive days (walking back from today) with at least one task completed —
    /// identical algorithm to <see cref="Goal.GetCurrentStreak"/>, just over
    /// <see cref="TaskItem.CompletedAt"/> instead of a <c>GoalCompletion</c> log. Uses
    /// <see cref="LocalDate"/> (not the raw UTC date) since a task completed just after
    /// midnight local time but before midnight UTC would otherwise silently count toward the
    /// wrong day.
    /// </summary>
    private int ComputeCurrentStreak(IReadOnlyList<TaskItem> tasks, DateOnly today)
    {
        var completedDates = tasks
            .Where(t => t.IsCompleted && t.CompletedAt is not null)
            .Select(t => LocalDate(t.CompletedAt!.Value))
            .ToHashSet();

        var cursor = completedDates.Contains(today) ? today : today.AddDays(-1);
        if (!completedDates.Contains(cursor))
        {
            return 0;
        }

        var streak = 0;
        while (completedDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    /// <summary><see cref="TaskItem.CompletedAt"/> and <see cref="FocusSession.StartedAt"/> are always set from <c>DateTime.UtcNow</c> — this converts a value like either of those to the same local timezone <see cref="TimeProvider.GetLocalNow"/> already uses everywhere else in this app, so an event near midnight lands on the calendar day the user actually experienced it as.</summary>
    private DateOnly LocalDate(DateTime utcValue) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcValue, DateTimeKind.Utc), timeProvider.LocalTimeZone));

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    /// <summary>Sunday-start, matching <c>WeekViewModel</c>'s exact convention (<c>target.AddDays(-(int)target.DayOfWeek)</c>).</summary>
    private static DateOnly StartOfWeek(DateOnly date) => date.AddDays(-(int)date.DayOfWeek);
}
