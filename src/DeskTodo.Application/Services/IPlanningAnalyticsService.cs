namespace DeskTodo.Application.Services;

/// <summary>Feature 51's Project Health Score result for one project.</summary>
public sealed record ProjectHealthReport(Guid ProjectId, string ProjectName, string Status, IReadOnlyList<string> Reasons);

/// <summary>Feature 52's Deadline Risk Detection result for one task.</summary>
public sealed record DeadlineRisk(Guid TaskId, string Title, string RiskLevel, string Reason);

/// <summary>One day of Feature 53's Workload Heatmap.</summary>
public sealed record WorkloadDay(DateOnly Date, double PlannedHours, double CapacityHours, bool IsOverloaded);

/// <summary>Feature 55's Time Estimation Accuracy for one grouping (a category, or "Overall").</summary>
public sealed record EstimationAccuracy(string GroupName, double AccuracyPercent, int SampleSize);

/// <summary>Feature 56's Task Cost Tracking totals — only meaningful once an hourly rate is configured.</summary>
public sealed record CostSummary(decimal EstimatedCost, decimal ActualCost);

/// <summary>
/// Features 51-56 (Roadmap-39-100.md), except 49 and 54 — a family of read-only analytics
/// computed from data this app already persists (tasks, projects, the existing time-tracking
/// fields), no new schema beyond Feature 50's Milestone.ProjectId/Order and Feature 54's two
/// AppSettings fields (WorkingHoursPerDay, HourlyRate). See each method's own doc comment for
/// which spec feature it implements and how the formula was scoped down from the spec's own
/// "example formula" (deliberately configurable-in-spirit, not hardcoded to one "correct"
/// weighting — see <see cref="GetProjectHealthAsync"/>).
/// </summary>
public interface IPlanningAnalyticsService
{
    /// <summary>
    /// Feature 51 — Healthy/Warning/Critical per project, from completion %, overdue %, blocked
    /// count and recent activity. "Unknown" for a project with no tasks at all (nothing to
    /// assess). The spec explicitly says "do not make the exact formula hardcoded" — the exact
    /// thresholds (documented on <c>PlanningAnalyticsService</c>) are one reasonable default,
    /// not the only correct one; a future pass could make them user-configurable without
    /// changing this method's shape.
    /// </summary>
    Task<IReadOnlyList<ProjectHealthReport>> GetProjectHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>Feature 52 — High/Medium/Low risk for incomplete tasks with a due date, from remaining estimated effort vs. time left and blocked status.</summary>
    Task<IReadOnlyList<DeadlineRisk>> GetDeadlineRisksAsync(CancellationToken cancellationToken = default);

    /// <summary>Feature 53 — planned hours (sum of EstimatedMinutes for that day's tasks) vs. AppSettings.WorkingHoursPerDay, for the next <paramref name="days"/> days starting today.</summary>
    Task<IReadOnlyList<WorkloadDay>> GetWorkloadForecastAsync(int days, DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>Feature 55 — accuracy (Estimated/Actual, capped for display) across every completed task with both fields set, overall and broken down by category.</summary>
    Task<IReadOnlyList<EstimationAccuracy>> GetEstimationAccuracyAsync(CancellationToken cancellationToken = default);

    /// <summary>Feature 56 — null when no hourly rate is configured (cost tracking is off by default, per the spec's own "keep this optional" note).</summary>
    Task<CostSummary?> GetCostSummaryAsync(CancellationToken cancellationToken = default);
}
