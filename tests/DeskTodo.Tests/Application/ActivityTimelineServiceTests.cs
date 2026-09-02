using DeskTodo.Application.Abstractions;
using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Moq;

namespace DeskTodo.Tests.Application;

public class ActivityTimelineServiceTests
{
    private readonly Mock<ITaskHistoryRepository> _taskHistoryRepository = new();
    private readonly Mock<IFocusSessionService> _focusSessionService = new();
    private readonly Mock<IGoalRepository> _goalRepository = new();
    private readonly ActivityTimelineService _sut;

    public ActivityTimelineServiceTests()
    {
        _taskHistoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _focusSessionService.Setup(s => s.GetAllSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _goalRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _sut = new ActivityTimelineService(_taskHistoryRepository.Object, _focusSessionService.Object, _goalRepository.Object);
    }

    [Fact]
    public async Task GetRecentActivityAsync_IncludesTaskHistoryEntries()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Write docs" };
        var entry = new TaskHistory { Task = task, Action = TaskHistoryAction.Completed, Timestamp = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc) };
        _taskHistoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([entry]);

        var results = await _sut.GetRecentActivityAsync();

        var activity = Assert.Single(results);
        Assert.Equal("Task", activity.Category);
        Assert.Contains("Write docs", activity.Description);
    }

    [Fact]
    public async Task GetRecentActivityAsync_IncludesFocusSessions()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Deep work" };
        var session = new FocusSession { Type = FocusSessionType.Pomodoro, Task = task, StartedAt = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc), EndedAt = new DateTime(2026, 8, 1, 9, 25, 0, DateTimeKind.Utc), DurationMinutes = 25 };
        _focusSessionService.Setup(s => s.GetAllSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([session]);

        var results = await _sut.GetRecentActivityAsync();

        var activity = Assert.Single(results);
        Assert.Equal("Focus", activity.Category);
        Assert.Contains("25m", activity.Description);
        Assert.Contains("Deep work", activity.Description);
    }

    [Fact]
    public async Task GetRecentActivityAsync_IncludesGoalCompletions()
    {
        var goal = new Goal { Name = "Read daily" };
        goal.Completions.Add(new GoalCompletion { GoalId = goal.Id, CompletedDate = new DateOnly(2026, 8, 1) });
        _goalRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([goal]);

        var results = await _sut.GetRecentActivityAsync();

        var activity = Assert.Single(results);
        Assert.Equal("Goal", activity.Category);
        Assert.Contains("Read daily", activity.Description);
    }

    [Fact]
    public async Task GetRecentActivityAsync_OrdersEverythingByTimestampDescending()
    {
        var older = new TaskHistory { Task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Older" }, Action = TaskHistoryAction.Created, Timestamp = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc) };
        var newer = new TaskHistory { Task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Newer" }, Action = TaskHistoryAction.Created, Timestamp = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc) };
        _taskHistoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([older, newer]);

        var results = await _sut.GetRecentActivityAsync();

        Assert.Contains("Newer", results[0].Description);
        Assert.Contains("Older", results[1].Description);
    }

    [Fact]
    public async Task GetRecentActivityAsync_RespectsTheLimit()
    {
        var entries = Enumerable.Range(0, 10)
            .Select(i => new TaskHistory { Task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = $"Task {i}" }, Action = TaskHistoryAction.Created, Timestamp = new DateTime(2026, 8, 1).AddMinutes(i) })
            .ToList();
        _taskHistoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entries);

        var results = await _sut.GetRecentActivityAsync(limit: 3);

        Assert.Equal(3, results.Count);
    }
}
