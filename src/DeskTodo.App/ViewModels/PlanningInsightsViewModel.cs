using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DeskTodo.Application.Services;
using Microsoft.Extensions.Logging;

namespace DeskTodo.App.ViewModels;

/// <summary>
/// Backs the Planning Insights window — Features 51 (Project Health), 52 (Deadline Risk), 53
/// (Workload Heatmap), 55 (Time Estimation Accuracy) and 56 (Task Cost Tracking), Roadmap-39-100.md.
/// One window, not five, since all five are read-only reports over the same underlying task
/// data with no cross-navigation needs of their own.
/// </summary>
public sealed partial class PlanningInsightsViewModel(IPlanningAnalyticsService planningAnalyticsService, TimeProvider timeProvider, ILogger<PlanningInsightsViewModel> logger) : ViewModelBase
{
    public ObservableCollection<ProjectHealthOption> ProjectHealth { get; } = [];

    public ObservableCollection<DeadlineRiskOption> DeadlineRisks { get; } = [];

    public ObservableCollection<WorkloadDayOption> Workload { get; } = [];

    public ObservableCollection<EstimationAccuracyOption> EstimationAccuracy { get; } = [];

    [ObservableProperty]
    public partial string? CostSummaryDisplay { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

            var health = await planningAnalyticsService.GetProjectHealthAsync(cancellationToken);
            ProjectHealth.Clear();
            foreach (var report in health)
            {
                ProjectHealth.Add(new ProjectHealthOption(report.ProjectName, report.Status, string.Join(" · ", report.Reasons)));
            }

            var risks = await planningAnalyticsService.GetDeadlineRisksAsync(cancellationToken);
            DeadlineRisks.Clear();
            foreach (var risk in risks)
            {
                DeadlineRisks.Add(new DeadlineRiskOption(risk.Title, risk.RiskLevel, risk.Reason));
            }

            var workload = await planningAnalyticsService.GetWorkloadForecastAsync(7, today, cancellationToken);
            Workload.Clear();
            foreach (var day in workload)
            {
                var display = $"{day.PlannedHours:0.#}h / {day.CapacityHours:0.#}h";
                Workload.Add(new WorkloadDayOption(day.Date.ToString("ddd, MMM d"), day.IsOverloaded ? $"{display} OVERLOAD" : display, day.IsOverloaded));
            }

            var accuracy = await planningAnalyticsService.GetEstimationAccuracyAsync(cancellationToken);
            EstimationAccuracy.Clear();
            foreach (var entry in accuracy)
            {
                EstimationAccuracy.Add(new EstimationAccuracyOption(entry.GroupName, $"{entry.AccuracyPercent:0}%", entry.SampleSize));
            }

            var cost = await planningAnalyticsService.GetCostSummaryAsync(cancellationToken);
            CostSummaryDisplay = cost is { } c ? $"Estimated: {c.EstimatedCost:C} · Actual: {c.ActualCost:C}" : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Planning Insights");
        }
    }
}
