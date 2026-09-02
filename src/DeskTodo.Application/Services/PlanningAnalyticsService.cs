using DeskTodo.Application.Abstractions;

namespace DeskTodo.Application.Services;

/// <inheritdoc cref="IPlanningAnalyticsService"/>
public sealed class PlanningAnalyticsService(ITaskService taskService, IProjectService projectService, ISettingsService settingsService) : IPlanningAnalyticsService
{
    /// <summary>
    /// Health formula (deliberately simple, documented rather than hidden — see the interface's
    /// own "do not hardcode" note): start at 100, subtract 2 points per % overdue and 5 points
    /// per blocked task, floor at 0. Critical &lt; 50, Warning &lt; 80, else Healthy. "Recent
    /// activity" and "workload" from the spec's fuller factor list aren't folded into the score
    /// itself in this pass (each needs its own cross-project query this pass keeps out of the
    /// hot path) — Deadline Risk (Feature 52) and the Workload Heatmap (Feature 53) cover that
    /// ground as their own dedicated views instead.
    /// </summary>
    public async Task<IReadOnlyList<ProjectHealthReport>> GetProjectHealthAsync(CancellationToken cancellationToken = default)
    {
        var projects = await projectService.GetProjectsAsync(cancellationToken);
        var reports = new List<ProjectHealthReport>();

        foreach (var project in projects.Where(p => !p.IsArchived))
        {
            var tasks = project.Tasks.Where(t => !t.IsDeleted).ToList();
            if (tasks.Count == 0)
            {
                reports.Add(new ProjectHealthReport(project.Id, project.Name, "Unknown", ["No tasks yet"]));
                continue;
            }

            var completed = tasks.Count(t => t.IsCompleted);
            var completionPercent = 100.0 * completed / tasks.Count;
            var overdueTasks = tasks.Where(t => t.IsOverdue).ToList();
            var overduePercent = 100.0 * overdueTasks.Count / tasks.Count;
            var blockedCount = tasks.Count(t => t.IsBlocked);

            var score = 100.0 - (2 * overduePercent) - (5 * blockedCount);
            score = Math.Max(0, score);

            var status = score switch
            {
                < 50 => "Critical",
                < 80 => "Warning",
                _ => "Healthy",
            };

            var reasons = new List<string> { $"{completionPercent:0}% complete ({completed}/{tasks.Count} tasks)" };
            if (overdueTasks.Count > 0)
            {
                reasons.Add($"{overdueTasks.Count} overdue task(s)");
            }

            if (blockedCount > 0)
            {
                reasons.Add($"{blockedCount} blocked task(s)");
            }

            reports.Add(new ProjectHealthReport(project.Id, project.Name, status, reasons));
        }

        return reports;
    }

    /// <summary>
    /// Risk formula: a blocked task is always High risk (it can't even start). Otherwise,
    /// compares remaining estimated effort (EstimatedMinutes - ActualMinutes, floored at the
    /// full estimate if no time has been logged yet) against the time left until the due date —
    /// this pass doesn't model "available working hours per remaining day" (Feature 54's
    /// capacity profile is one global daily figure, not a per-task allocation), so this is a
    /// simpler "would the remaining estimate alone fit in the time left" check, not the spec's
    /// fuller capacity-aware model.
    /// </summary>
    public async Task<IReadOnlyList<DeadlineRisk>> GetDeadlineRisksAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await taskService.GetAllTasksAsync(cancellationToken);
        var risks = new List<DeadlineRisk>();
        var now = DateTime.UtcNow;

        foreach (var task in tasks.Where(t => !t.IsCompleted && t.DueDate.HasValue))
        {
            var dueDate = task.DueDate!.Value;
            if (dueDate < now)
            {
                continue; // already overdue — that's Feature 46/existing overdue handling's job, not a "risk of becoming" overdue.
            }

            if (task.IsBlocked)
            {
                risks.Add(new DeadlineRisk(task.Id, task.Title, "High", "Blocked by another task"));
                continue;
            }

            var remainingMinutes = Math.Max(0, (task.EstimatedMinutes ?? 0) - (task.ActualMinutes ?? 0));
            if (remainingMinutes == 0)
            {
                continue; // no estimate logged, or already fully worked — nothing to assess risk against.
            }

            var hoursLeft = (dueDate - now).TotalHours;
            var remainingHours = remainingMinutes / 60.0;

            var riskLevel = remainingHours >= hoursLeft ? "High"
                : remainingHours >= hoursLeft * 0.5 ? "Medium"
                : "Low";

            if (riskLevel != "Low")
            {
                risks.Add(new DeadlineRisk(task.Id, task.Title, riskLevel, $"{remainingHours:0.#}h remaining, {hoursLeft:0.#}h until due"));
            }
        }

        return risks.OrderByDescending(r => r.RiskLevel == "High").ToList();
    }

    public async Task<IReadOnlyList<WorkloadDay>> GetWorkloadForecastAsync(int days, DateOnly today, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        var result = new List<WorkloadDay>();

        for (var i = 0; i < days; i++)
        {
            var date = today.AddDays(i);
            var dayTasks = await taskService.GetTasksForDateAsync(date, cancellationToken);
            var plannedHours = dayTasks.Where(t => !t.IsCompleted).Sum(t => (t.EstimatedMinutes ?? 0) / 60.0);
            result.Add(new WorkloadDay(date, plannedHours, settings.WorkingHoursPerDay, plannedHours > settings.WorkingHoursPerDay));
        }

        return result;
    }

    public async Task<IReadOnlyList<EstimationAccuracy>> GetEstimationAccuracyAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await taskService.GetAllTasksAsync(cancellationToken);
        var measurable = tasks.Where(t => t.EstimatedMinutes is > 0 && t.ActualMinutes is > 0).ToList();
        if (measurable.Count == 0)
        {
            return [];
        }

        var results = new List<EstimationAccuracy>
        {
            new("Overall", ComputeAccuracyPercent(measurable), measurable.Count),
        };

        var byCategory = measurable
            .GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in byCategory)
        {
            var items = group.ToList();
            results.Add(new EstimationAccuracy(group.Key, ComputeAccuracyPercent(items), items.Count));
        }

        return results;
    }

    /// <summary>100% means every task landed exactly on its estimate; below 100 means tasks ran over, above 100 means tasks came in under estimate — averaged per task, not summed, so one huge outlier task doesn't dominate the whole group's number.</summary>
    private static double ComputeAccuracyPercent(IReadOnlyCollection<Domain.Entities.TaskItem> tasks) =>
        tasks.Average(t => 100.0 * t.EstimatedMinutes!.Value / t.ActualMinutes!.Value);

    public async Task<CostSummary?> GetCostSummaryAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        if (settings.HourlyRate is not { } rate || rate <= 0)
        {
            return null;
        }

        var tasks = await taskService.GetAllTasksAsync(cancellationToken);
        var estimatedHours = (decimal)tasks.Sum(t => (t.EstimatedMinutes ?? 0) / 60.0);
        var actualHours = (decimal)tasks.Sum(t => (t.ActualMinutes ?? 0) / 60.0);

        return new CostSummary(estimatedHours * rate, actualHours * rate);
    }
}
