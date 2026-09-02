using DeskTodo.Application.Services;
using DeskTodo.Domain.Entities;
using DeskTodo.Domain.Enums;
using Moq;

namespace DeskTodo.Tests.Application;

public class AchievementServiceTests
{
    private readonly Mock<ITaskService> _taskService = new();
    private readonly Mock<IFocusSessionService> _focusSessionService = new();
    private readonly Mock<IProjectService> _projectService = new();
    private readonly Mock<IMilestoneService> _milestoneService = new();
    private readonly AchievementService _sut;

    public AchievementServiceTests()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TaskItem>());
        _focusSessionService.Setup(s => s.GetAllSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<FocusSession>());
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Project>());
        _milestoneService.Setup(s => s.GetMilestonesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Milestone>());
        _sut = new AchievementService(_taskService.Object, _focusSessionService.Object, _projectService.Object, _milestoneService.Object);
    }

    private static TaskItem MakeCompletedTask()
    {
        var task = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Task" };
        task.Complete();
        return task;
    }

    [Fact]
    public async Task GetAchievementsAsync_WithNoData_UnlocksNothingExceptWellOrganized()
    {
        var results = await _sut.GetAchievementsAsync();

        Assert.False(results.Single(a => a.Title == "First Steps").IsUnlocked);
        Assert.False(results.Single(a => a.Title == "Century").IsUnlocked);
        // No overdue tasks (there are no tasks at all) - "Well Organized" is trivially true.
        Assert.True(results.Single(a => a.Title == "Well Organized").IsUnlocked);
    }

    [Fact]
    public async Task GetAchievementsAsync_WithOneCompletedTask_UnlocksFirstSteps_ButNotCentury()
    {
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([MakeCompletedTask()]);

        var results = await _sut.GetAchievementsAsync();

        Assert.True(results.Single(a => a.Title == "First Steps").IsUnlocked);
        Assert.False(results.Single(a => a.Title == "Century").IsUnlocked);
    }

    [Fact]
    public async Task GetAchievementsAsync_With100CompletedTasks_UnlocksCentury()
    {
        var tasks = Enumerable.Range(0, 100).Select(_ => MakeCompletedTask()).ToList();
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        var results = await _sut.GetAchievementsAsync();

        Assert.True(results.Single(a => a.Title == "Century").IsUnlocked);
    }

    [Fact]
    public async Task GetAchievementsAsync_With50FocusHoursLogged_UnlocksThatAchievement()
    {
        var sessions = new List<FocusSession>
        {
            new() { Type = FocusSessionType.CountdownTimer, StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow, DurationMinutes = 50 * 60 },
        };
        _focusSessionService.Setup(s => s.GetAllSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sessions);

        var results = await _sut.GetAchievementsAsync();

        Assert.True(results.Single(a => a.Title == "50 Focus Hours").IsUnlocked);
    }

    [Fact]
    public async Task GetAchievementsAsync_WithAProjectWhoseTasksAreAllComplete_UnlocksProjectFinisher()
    {
        var project = new Project { Name = "Website", ColorHex = "#3B82F6" };
        project.Tasks.Add(MakeCompletedTask());
        _projectService.Setup(s => s.GetProjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([project]);

        var results = await _sut.GetAchievementsAsync();

        Assert.True(results.Single(a => a.Title == "Project Finisher").IsUnlocked);
    }

    [Fact]
    public async Task GetAchievementsAsync_WithAnOverdueTask_LocksWellOrganized()
    {
        var overdueTask = new TaskItem { PlanDate = new DateOnly(2026, 8, 1), Title = "Late", DueDate = DateTime.UtcNow.AddDays(-1) };
        _taskService.Setup(s => s.GetAllTasksAsync(It.IsAny<CancellationToken>())).ReturnsAsync([overdueTask]);

        var results = await _sut.GetAchievementsAsync();

        Assert.False(results.Single(a => a.Title == "Well Organized").IsUnlocked);
    }
}
