using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Application.Settings;
using DeskTodo.Domain.Entities;
using Moq;

namespace DeskTodo.Tests.Application;

public class PlanningAnalyticsServiceTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<IProjectService> _projectService = new();
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly PlanningAnalyticsService _sut;

    public PlanningAnalyticsServiceTests()
    {
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TaskItem>());
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Project>());
        _sut = new PlanningAnalyticsService(_taskService.Object, _projectService.Object, _settingsService.Object);
    }

    private static TaskItem MakeTask(bool completed = false, bool overdue = false, int? estimated = null, int? actual = null, string? categoryName = null)
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Task", EstimatedMinutes = estimated, ActualMinutes = actual };
        if (completed)
        {
            task.Complete();
        }

        if (overdue)
        {
            task.DueDate = DateTime.UtcNow.AddDays(-1);
        }

        if (categoryName is not null)
        {
            task.Category = new Category { Name = categoryName, ColorHex = "#000000" };
        }

        return task;
    }

    [Fact]
    public async Task GetProjectHealthAsync_WithNoTasks_ReportsUnknown()
    {
        var project = new Project { Name = "Empty", ColorHex = "#3B82F6" };
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        var results = await _sut.GetProjectHealthAsync();

        var report = Assert.Single(results);
        Assert.Equal("Unknown", report.Status);
    }

    [Fact]
    public async Task GetProjectHealthAsync_ExcludesArchivedProjects()
    {
        var project = new Project { Name = "Archived", ColorHex = "#3B82F6", IsArchived = true };
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        var results = await _sut.GetProjectHealthAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetProjectHealthAsync_WithAllTasksComplete_AndNoneOverdue_ReportsHealthy()
    {
        var project = new Project { Name = "On Track", ColorHex = "#3B82F6" };
        project.Tasks.Add(MakeTask(completed: true));
        project.Tasks.Add(MakeTask(completed: true));
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        var results = await _sut.GetProjectHealthAsync();

        Assert.Equal("Healthy", results[0].Status);
    }

    [Fact]
    public async Task GetProjectHealthAsync_WithManyOverdueTasks_ReportsCriticalOrWarning()
    {
        var project = new Project { Name = "Behind", ColorHex = "#3B82F6" };
        for (var i = 0; i < 10; i++)
        {
            project.Tasks.Add(MakeTask(overdue: true));
        }

        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        var results = await _sut.GetProjectHealthAsync();

        Assert.NotEqual("Healthy", results[0].Status);
        Assert.Contains("overdue", string.Join(" ", results[0].Reasons));
    }

    [Fact]
    public async Task GetDeadlineRisksAsync_SkipsCompletedAndTasksWithNoDueDate()
    {
        var completed = MakeTask(completed: true);
        completed.DueDate = DateTime.UtcNow.AddHours(1);
        var noDueDate = MakeTask();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([completed, noDueDate]);

        var results = await _sut.GetDeadlineRisksAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetDeadlineRisksAsync_FlagsATightDeadlineAsHighRisk()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Tight", EstimatedMinutes = 600, DueDate = DateTime.UtcNow.AddHours(1) };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        var results = await _sut.GetDeadlineRisksAsync();

        var risk = Assert.Single(results);
        Assert.Equal("High", risk.RiskLevel);
    }

    [Fact]
    public async Task GetDeadlineRisksAsync_WithPlentyOfTimeLeft_ReportsNoRisk()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Plenty of time", EstimatedMinutes = 30, DueDate = DateTime.UtcNow.AddDays(30) };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        var results = await _sut.GetDeadlineRisksAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetWorkloadForecastAsync_SumsEstimatedMinutesPerDay_AndFlagsOverload()
    {
        var today = new DateOnly(2026, 8, 1);
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { WorkingHoursPerDay = 8 });
        _taskService.Setup(s => s.GetTasksForDateAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeTask(estimated: 6 * 60), MakeTask(estimated: 4 * 60)]);
        _taskService.Setup(s => s.GetTasksForDateAsync(today.AddDays(1), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var results = await _sut.GetWorkloadForecastAsync(2, today);

        Assert.Equal(10, results[0].PlannedHours);
        Assert.True(results[0].IsOverloaded);
        Assert.False(results[1].IsOverloaded);
    }

    [Fact]
    public async Task GetEstimationAccuracyAsync_WithNoMeasurableTasks_ReturnsEmpty()
    {
        var results = await _sut.GetEstimationAccuracyAsync();

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetEstimationAccuracyAsync_ComputesOverallAndPerCategoryAccuracy()
    {
        var tasks = new List<TaskItem>
        {
            MakeTask(estimated: 60, actual: 60, categoryName: "Bugs"), // 100%
            MakeTask(estimated: 60, actual: 120, categoryName: "Bugs"), // 50%
            MakeTask(estimated: 30, actual: 30, categoryName: "Docs"), // 100%
        };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        var results = await _sut.GetEstimationAccuracyAsync();

        var overall = results.Single(r => r.GroupName == "Overall");
        Assert.Equal(3, overall.SampleSize);
        var bugs = results.Single(r => r.GroupName == "Bugs");
        Assert.Equal(75, bugs.AccuracyPercent);
        var docs = results.Single(r => r.GroupName == "Docs");
        Assert.Equal(100, docs.AccuracyPercent);
    }

    [Fact]
    public async Task GetCostSummaryAsync_WithNoHourlyRateConfigured_ReturnsNull()
    {
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { HourlyRate = null });

        var result = await _sut.GetCostSummaryAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCostSummaryAsync_WithAnHourlyRate_ComputesEstimatedAndActualCost()
    {
        _settingsService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { HourlyRate = 100m });
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeTask(estimated: 120, actual: 60)]);

        var result = await _sut.GetCostSummaryAsync();

        Assert.NotNull(result);
        Assert.Equal(200m, result!.EstimatedCost);
        Assert.Equal(100m, result.ActualCost);
    }
}
