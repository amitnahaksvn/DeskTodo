using System.Reflection;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using DeskTodo.Tests.ViewModels;
using Moq;

namespace DeskTodo.Tests.Application;

public class AnalyticsServiceTests
{
    // 2026-08-12 is a Wednesday. LocalTimeZone pinned to UTC (see FakeTimeProvider) so
    // "today" in every AnalyticsService calculation matches this exactly, with no
    // UTC-vs-local ambiguity to reason about in assertions.
    private static readonly DateOnly Today = new(2026, 8, 12);
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(Today.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero));
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<IFocusSessionService> _focusSessionService = new();
    private readonly AnalyticsService _sut;

    public AnalyticsServiceTests()
    {
        _sut = new AnalyticsService(_taskService.Object, _focusSessionService.Object, _timeProvider);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TaskItem>());
        _focusSessionService.Setup(s => s.GetAllSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<FocusSession>());
    }

    private static TaskItem MakeTask(DateOnly planDate, string title = "Task", Guid? categoryId = null, Category? category = null) =>
        new() { PlanDate = planDate, Title = title, CategoryId = categoryId, Category = category };

    /// <summary>
    /// <see cref="TaskItem.Complete"/> always stamps the real <c>DateTime.UtcNow</c> — a
    /// deliberate domain invariant (you can't complete a task in the past) with no public
    /// override. Streak/heat-map tests genuinely need specific historical completion dates
    /// to exercise the walk-backward algorithm meaningfully, so this reaches past that
    /// private setter via reflection — test-only, and exactly why it's not a public API.
    /// </summary>
    private static void CompleteOn(TaskItem task, DateOnly localDate)
    {
        task.Complete();
        var utcCompletedAt = localDate.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
        typeof(TaskItem).GetProperty(nameof(TaskItem.CompletedAt))!.SetValue(task, utcCompletedAt);
    }

    private static FocusSession MakeSession(DateOnly localDate, int durationMinutes, Guid? taskId = null) => new()
    {
        Type = FocusSessionType.Stopwatch,
        TaskId = taskId,
        StartedAt = localDate.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
        EndedAt = localDate.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc).AddMinutes(durationMinutes),
        DurationMinutes = durationMinutes,
    };

    [Fact]
    public async Task GetSummaryAsync_ComputesWeeklyMonthlyAndOverallCompletionRates()
    {
        var weekStart = Today.AddDays(-(int)Today.DayOfWeek); // Sunday of this week
        var inWeekDone = MakeTask(weekStart.AddDays(1));
        inWeekDone.Complete();
        var inWeekOpen = MakeTask(weekStart.AddDays(2));
        var outsideWeek = MakeTask(weekStart.AddDays(-10));
        outsideWeek.Complete();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([inWeekDone, inWeekOpen, outsideWeek]);

        var summary = await _sut.GetSummaryAsync();

        Assert.Equal(50, summary.WeeklyCompletionRate);
        Assert.Equal(2.0 / 3 * 100, summary.OverallCompletionRate, precision: 3);
    }

    [Fact]
    public async Task GetSummaryAsync_WithNoTasksInPeriod_ReturnsZeroNotDivideByZero()
    {
        var summary = await _sut.GetSummaryAsync();

        Assert.Equal(0, summary.WeeklyCompletionRate);
        Assert.Equal(0, summary.MonthlyCompletionRate);
        Assert.Equal(0, summary.OverallCompletionRate);
    }

    [Fact]
    public async Task GetSummaryAsync_ExcludesArchivedTasks()
    {
        var archived = MakeTask(Today);
        archived.Archive();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([archived]);

        var summary = await _sut.GetSummaryAsync();

        Assert.Equal(0, summary.OverallCompletionRate); // Archived task excluded, so 0 tasks scoped -> 0, not counted at all.
    }

    [Fact]
    public async Task GetSummaryAsync_StreakWalksBackConsecutiveDaysFromToday()
    {
        var completedToday = MakeTask(Today);
        CompleteOn(completedToday, Today);
        var completedYesterday = MakeTask(Today.AddDays(-1));
        CompleteOn(completedYesterday, Today.AddDays(-1));
        var completedTwoDaysAgo = MakeTask(Today.AddDays(-2));
        CompleteOn(completedTwoDaysAgo, Today.AddDays(-2));
        // Gap at 3 days ago — nothing completed then, so the streak stops at 3.
        var completedFourDaysAgo = MakeTask(Today.AddDays(-4));
        CompleteOn(completedFourDaysAgo, Today.AddDays(-4));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([completedToday, completedYesterday, completedTwoDaysAgo, completedFourDaysAgo]);

        var summary = await _sut.GetSummaryAsync();

        Assert.Equal(3, summary.CurrentStreakDays);
    }

    [Fact]
    public async Task GetSummaryAsync_StreakIsZero_WhenNothingCompletedTodayOrYesterday()
    {
        var completedThreeDaysAgo = MakeTask(Today.AddDays(-3));
        CompleteOn(completedThreeDaysAgo, Today.AddDays(-3));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([completedThreeDaysAgo]);

        var summary = await _sut.GetSummaryAsync();

        Assert.Equal(0, summary.CurrentStreakDays);
    }

    [Fact]
    public async Task GetSummaryAsync_SumsFocusMinutesForThisWeekAndAllTime()
    {
        var weekStart = Today.AddDays(-(int)Today.DayOfWeek);
        var thisWeekSession = MakeSession(weekStart.AddDays(1), 25);
        var lastWeekSession = MakeSession(weekStart.AddDays(-3), 50);
        _focusSessionService.Setup(s => s.GetAllSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([thisWeekSession, lastWeekSession]);

        var summary = await _sut.GetSummaryAsync();

        Assert.Equal(25, summary.FocusMinutesThisWeek);
        Assert.Equal(75, summary.FocusMinutesAllTime);
    }

    [Fact]
    public async Task GetHeatMapDataAsync_ReturnsOneRowPerDayInRange_WithCorrectCounts()
    {
        var completed = MakeTask(Today.AddDays(-1));
        CompleteOn(completed, Today.AddDays(-1));
        var stillOpen = MakeTask(Today.AddDays(-1));
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([completed, stillOpen]);

        var days = await _sut.GetHeatMapDataAsync(Today.AddDays(-2), Today, default);

        Assert.Equal(3, days.Count);
        var yesterday = days.Single(d => d.Date == Today.AddDays(-1));
        Assert.Equal(1, yesterday.CompletedCount);
        Assert.Equal(2, yesterday.TotalCount);
        Assert.All(days.Where(d => d.Date != Today.AddDays(-1)), d => Assert.Equal(0, d.CompletedCount));
    }

    [Fact]
    public async Task GetCategoryAnalyticsAsync_GroupsByCategory_AndIncludesNoCategoryBucket()
    {
        var work = new Category { Name = "Work", ColorHex = "#3B82F6" };
        var withCategory = MakeTask(Today, categoryId: work.Id, category: work);
        withCategory.Complete();
        var withoutCategory = MakeTask(Today);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([withCategory, withoutCategory]);

        var results = await _sut.GetCategoryAnalyticsAsync();

        Assert.Equal(2, results.Count);
        var workRow = results.Single(r => r.CategoryId == work.Id);
        Assert.Equal("Work", workRow.CategoryName);
        Assert.Equal(1, workRow.TotalCount);
        Assert.Equal(1, workRow.CompletedCount);
        Assert.Equal(100, workRow.CompletionRate);
        var noCategoryRow = results.Single(r => r.CategoryId == null);
        Assert.Equal("No Category", noCategoryRow.CategoryName);
    }

    [Fact]
    public async Task GetCategoryAnalyticsAsync_SumsFocusMinutesPerCategory()
    {
        var work = new Category { Name = "Work", ColorHex = "#3B82F6" };
        var task = MakeTask(Today, categoryId: work.Id, category: work);
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([task]);
        _focusSessionService.Setup(s => s.GetAllSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([MakeSession(Today, 20, task.Id), MakeSession(Today, 15, task.Id)]);

        var results = await _sut.GetCategoryAnalyticsAsync();

        Assert.Equal(35, results.Single().FocusMinutes);
    }

    [Fact]
    public async Task GenerateReportAsync_ListsCompletedAndOpenTasksSeparately_WithCorrectSummary()
    {
        var completed = MakeTask(Today, "Ship the release");
        completed.Complete();
        var open = MakeTask(Today, "Write docs");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([completed, open]);
        _focusSessionService.Setup(s => s.GetAllSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeSession(Today, 30)]);

        var report = await _sut.GenerateReportAsync(Today, Today);

        Assert.Contains("1 / 2 (50%)", report);
        Assert.Contains("30 minutes", report);
        Assert.Contains("[x] Ship the release", report);
        Assert.Contains("[ ] Write docs", report);
    }

    [Fact]
    public async Task GenerateReportAsync_OutsidePeriod_TasksAreExcluded()
    {
        var outsideTask = MakeTask(Today.AddDays(-30), "Old task");
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([outsideTask]);

        var report = await _sut.GenerateReportAsync(Today, Today);

        Assert.DoesNotContain("Old task", report);
        Assert.Contains("_None._", report);
    }
}
